using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    public class TD_AuraBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        
        // Maps the ability spec to the cancellation token for its scanning loop
        private readonly Dictionary<GameplayAbilitySpec, CancellationTokenSource> _activeAuras = new Dictionary<GameplayAbilitySpec, CancellationTokenSource>();

        [Inject]
        public TD_AuraBehaviour(IEnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) => true;

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var auraData = data as TD_AuraData;
            if (auraData == null || asc?.Avatar == null) return;

            // Start the sweep loop via UniTask
            var cts = new CancellationTokenSource();
            _activeAuras[spec] = cts;
            
            RunAuraLoopAsync(asc, spec, auraData, cts.Token).Forget();
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (_activeAuras.TryGetValue(spec, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _activeAuras.Remove(spec);
            }
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            OnEnded(data, asc, spec);
        }

        private async UniTaskVoid RunAuraLoopAsync(AbilitySystemComponent ownerASC, GameplayAbilitySpec spec, TD_AuraData data, CancellationToken token)
        {
            HashSet<int> currentTargets = new HashSet<int>();
            Dictionary<int, ActiveGameplayEffect> appliedEffects = new Dictionary<int, ActiveGameplayEffect>();
            List<int> buffer = new List<int>(32);

            try
            {
                while (!token.IsCancellationRequested && spec.IsActive && ownerASC.Avatar != null && ownerASC.Avatar.IsValid)
                {
                    buffer.Clear();
                    _enemyManager.GetEnemiesInRange(ownerASC.Position, data.radius, buffer);

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

                    await UniTask.Delay(TimeSpan.FromSeconds(data.tickInterval), cancellationToken: token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Final Cleanup: Remove all applied effects when aura ends
                foreach (var kvp in appliedEffects)
                {
                    if (_enemyManager.TryGetEnemyASC(kvp.Key, out var targetASC))
                    {
                        targetASC.RemoveGameplayEffect(kvp.Value);
                    }
                }
                appliedEffects.Clear();
                currentTargets.Clear();
            }
        }
    }
}
