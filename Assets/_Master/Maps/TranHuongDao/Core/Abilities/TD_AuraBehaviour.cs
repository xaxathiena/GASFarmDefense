using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FD.Modules.VFX;
using GAS;
using UnityEngine;
using VContainer;

namespace Abel.TranHuongDao.Core.Abilities
{
    public class TD_AuraBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        private readonly IVFXManager _vfxManager;

        private class AuraInstance
        {
            public CancellationTokenSource cts;
            public int persistentVfxHandle = -1;
            public Vector3 centerPos;
        }

        // Maps the ability spec to a list of active instances (for overlapping/multi-activation)
        private readonly Dictionary<GameplayAbilitySpec, List<AuraInstance>> _activeAuras = new Dictionary<GameplayAbilitySpec, List<AuraInstance>>();

        [Inject]
        public TD_AuraBehaviour(IEnemyManager enemyManager, IVFXManager vfxManager)
        {
            _enemyManager = enemyManager;
            _vfxManager = vfxManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) => true;

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var auraData = data as TD_AuraData;
            if (auraData == null || asc?.Avatar == null) return;

            // Start a new instance for this activation
            var instance = new AuraInstance
            {
                cts = new CancellationTokenSource(),
                centerPos = spec.TargetContext != null ? spec.TargetContext.Position : asc.Position
            };

            if (!_activeAuras.TryGetValue(spec, out var list))
            {
                list = new List<AuraInstance>();
                _activeAuras[spec] = list;
            }
            list.Add(instance);

            RunAuraLoopAsync(asc, spec, auraData, instance).Forget();
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (_activeAuras.TryGetValue(spec, out var list))
            {
                foreach (var instance in list)
                {
                    instance.cts.Cancel();
                    instance.cts.Dispose();
                    if (instance.persistentVfxHandle != -1)
                    {
                        _vfxManager.StopEffect(instance.persistentVfxHandle);
                    }
                }
                list.Clear();
                _activeAuras.Remove(spec);
            }
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            OnEnded(data, asc, spec);
        }

        private async UniTaskVoid RunAuraLoopAsync(AbilitySystemComponent ownerASC, GameplayAbilitySpec spec, TD_AuraData data, AuraInstance instance)
        {
            HashSet<int> currentTargets = new HashSet<int>();
            Dictionary<int, ActiveGameplayEffect> appliedEffects = new Dictionary<int, ActiveGameplayEffect>();
            List<int> buffer = new List<int>(32);
            var token = instance.cts.Token;

            // 1. Persistent VFX (once)
            if (!string.IsNullOrEmpty(data.vfxID) && data.vfxInterval <= 0)
            {
                instance.persistentVfxHandle = _vfxManager.PlayEffectAt(data.vfxID, instance.centerPos);
            }

            float elapsedTime = 0f;
            float nextVfxTime = 0f;

            try
            {
                while (!token.IsCancellationRequested && spec.IsActive && ownerASC.Avatar != null && ownerASC.Avatar.IsValid)
                {
                    // Update center position each frame (follow target if valid)
                    if (spec.TargetContext != null && spec.TargetContext.Avatar != null && spec.TargetContext.Avatar.IsValid)
                    {
                        instance.centerPos = spec.TargetContext.Position;
                    }
                    else if (spec.TargetContext == null)
                    {
                        instance.centerPos = ownerASC.Position;
                    }
                    // Else: target died, keep the last known centerPos

                    // Check duration
                    if (data.auraDuration > 0 && elapsedTime >= data.auraDuration)
                    {
                        // Cleanup this specific instance
                        CleanupInstance(spec, instance);

                        // If this was the last instance, notify GAS that the ability ended
                        if (!_activeAuras.TryGetValue(spec, out var list) || list.Count == 0)
                        {
                            ownerASC.EndAbility(spec.Definition);
                        }
                        return;
                    }

                    // Periodic VFX
                    if (!string.IsNullOrEmpty(data.vfxID) && data.vfxInterval > 0 && elapsedTime >= nextVfxTime)
                    {
                        int handle = _vfxManager.PlayEffectAt(data.vfxID, instance.centerPos);
                        if (data.vfxLifeTime > 0)
                        {
                            StopVfxAfterDelay(handle, data.vfxLifeTime, token).Forget();
                        }
                        nextVfxTime = elapsedTime + data.vfxInterval;
                    }

                    buffer.Clear();
                    _enemyManager.GetEnemiesInRange(instance.centerPos, data.radius, buffer);

                    // 1. Remove effects from enemies that left the range
                    List<int> toRemove = new List<int>();
                    foreach (var targetID in currentTargets)
                    {
                        if (!buffer.Contains(targetID))
                        {
                            if (appliedEffects.TryGetValue(targetID, out var activeEffect))
                            {
                                if (_enemyManager.TryGetEnemyASC(targetID, out var targetASC))
                                    targetASC.RemoveGameplayEffect(activeEffect);
                                appliedEffects.Remove(targetID);
                            }
                            toRemove.Add(targetID);
                        }
                    }
                    foreach (var id in toRemove) currentTargets.Remove(id);

                    // 2. Apply effects to new enemies that entered the range
                    foreach (var targetID in buffer)
                    {
                        if (!currentTargets.Contains(targetID))
                        {
                            if (_enemyManager.TryGetEnemyASC(targetID, out var targetASC))
                            {
                                var activeEffect = targetASC.ApplyGameplayEffectToSelf(data.auraEffect, ownerASC, 1f);
                                if (activeEffect != null)
                                {
                                    appliedEffects[targetID] = activeEffect;
                                    currentTargets.Add(targetID);
                                }
                            }
                        }
                    }

                    // For persistent VFX that should follow the chosen center
                    if (instance.persistentVfxHandle != -1)
                    {
                        _vfxManager.UpdateEffectPosition(instance.persistentVfxHandle, instance.centerPos);
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(data.tickInterval), cancellationToken: token);
                    elapsedTime += data.tickInterval;
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Final Cleanup for this instance
                foreach (var kvp in appliedEffects)
                {
                    if (_enemyManager.TryGetEnemyASC(kvp.Key, out var targetASC))
                    {
                        targetASC.RemoveGameplayEffect(kvp.Value);
                    }
                }
                appliedEffects.Clear();
                currentTargets.Clear();

                if (instance.persistentVfxHandle != -1)
                {
                    _vfxManager.StopEffect(instance.persistentVfxHandle);
                    instance.persistentVfxHandle = -1;
                }
            }
        }

        private void CleanupInstance(GameplayAbilitySpec spec, AuraInstance instance)
        {
            if (_activeAuras.TryGetValue(spec, out var list))
            {
                list.Remove(instance);
                if (list.Count == 0)
                {
                    _activeAuras.Remove(spec);
                }
            }
            instance.cts.Cancel();
            instance.cts.Dispose();
        }

        private async UniTaskVoid StopVfxAfterDelay(int handle, float delay, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
                _vfxManager.StopEffect(handle);
            }
            catch (OperationCanceledException)
            {
                _vfxManager.StopEffect(handle);
            }
        }
    }
}
