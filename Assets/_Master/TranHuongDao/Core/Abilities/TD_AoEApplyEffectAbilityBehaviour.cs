using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Implements an AoE Apply Effect Ability: Applies a Gameplay Effect to all enemies within a radius.
    /// Functions as a continuous Aura while active.
    /// </summary>
    public class TD_AoEApplyEffectAbilityBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;

        // Stores async task cancellation tokens per ability instance
        private readonly Dictionary<GameplayAbilitySpec, CancellationTokenSource> _activeSweeps = new Dictionary<GameplayAbilitySpec, CancellationTokenSource>();

        // Stores active targets and their effects per ability instance
        private readonly Dictionary<GameplayAbilitySpec, Dictionary<int, ActiveGameplayEffect>> _activeEffects =
            new Dictionary<GameplayAbilitySpec, Dictionary<int, ActiveGameplayEffect>>();

#if UNITY_EDITOR
        // Stores active gizmos per ability instance
        private readonly Dictionary<GameplayAbilitySpec, TD_AoEApplyEffectGizmo> _activeGizmos = new Dictionary<GameplayAbilitySpec, TD_AoEApplyEffectGizmo>();
#endif

        public TD_AoEApplyEffectAbilityBehaviour(IEnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var aoeData = data as TD_AoEApplyEffectAbilityData;
            if (aoeData == null || asc?.GetOwner() == null) return false;

            // Aura can always activate if owner exists
            return true;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var aoeData = data as TD_AoEApplyEffectAbilityData;
            if (aoeData == null || asc?.GetOwner() == null) return;

            // Optional: Spawn VFX (Aura rings)
            if (aoeData.castVfxPrefab != null)
            {
                // Instantiate as child of the owner so it moves with the aura
                UnityEngine.Object.Instantiate(aoeData.castVfxPrefab, asc.GetOwner().position, Quaternion.identity, asc.GetOwner());
            }

            _activeEffects[spec] = new Dictionary<int, ActiveGameplayEffect>();

#if UNITY_EDITOR
            // Add Gizmo to visualize the aura area in Editor
            if (asc.GetOwner() != null)
            {
                var gizmo = asc.GetOwner().gameObject.AddComponent<TD_AoEApplyEffectGizmo>();
                gizmo.Radius = aoeData.captureRadius;
                _activeGizmos[spec] = gizmo;
            }
#endif

            // Start the sweep loop via UniTask - completely independent of MonoBehaviour
            var cts = new CancellationTokenSource();
            _activeSweeps[spec] = cts;
            AuraSweepAsync(aoeData, asc, spec, cts.Token).Forget();
        }

        private async UniTaskVoid AuraSweepAsync(TD_AoEApplyEffectAbilityData aoeData, AbilitySystemComponent asc, GameplayAbilitySpec spec, CancellationToken token)
        {
            var targetEffects = _activeEffects[spec];
            List<int> sweepCache = new List<int>(32);
            List<int> toRemove = new List<int>(32);

            try
            {
                while (spec.IsActive && asc.GetOwner() != null && !token.IsCancellationRequested)
                {
                    Vector3 originPos = asc.GetOwner().position;
                    sweepCache.Clear();

                    // Depending on the target type, gather the IDs.
                    if (aoeData.targetType == EAuraTargetType.Enemies || aoeData.targetType == EAuraTargetType.Both)
                    {
                        _enemyManager.GetEnemiesInRange(originPos, aoeData.captureRadius, sweepCache);
                    }

                    // Check who left the aura
                    toRemove.Clear();
                    foreach (var targetID in targetEffects.Keys)
                    {
                        if (!sweepCache.Contains(targetID))
                        {
                            toRemove.Add(targetID);
                        }
                    }

                    // Remove effects for targets that left
                    foreach (int targetID in toRemove)
                    {
                        RemoveEffectFromTarget(targetID, targetEffects);
                    }

                    // Apply effects for new targets that entered
                    foreach (int targetID in sweepCache)
                    {
                        if (!targetEffects.ContainsKey(targetID))
                        {
                            ApplyEffectToTarget(targetID, aoeData, asc, targetEffects);
                        }
                    }

                    // Await using UniTask without creating GC allocations and independent of MonoBehaviour
                    await UniTask.Delay(System.TimeSpan.FromSeconds(aoeData.auraUpdateRate), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
                // Task was cleanly cancelled when the ability ended
            }
        }

        private void ApplyEffectToTarget(int targetID, TD_AoEApplyEffectAbilityData aoeData, AbilitySystemComponent asc, Dictionary<int, ActiveGameplayEffect> targetEffects)
        {
            if (aoeData.effectToApply == null) return;

            if (_enemyManager.TryGetEnemyASC(targetID, out var targetASC))
            {
                // Pass level 1f as simple default
                var activeEffect = targetASC.ApplyGameplayEffectToSelf(aoeData.effectToApply, asc, 1f);
                if (activeEffect != null)
                {
                    targetEffects[targetID] = activeEffect;
                }
            }
        }

        private void RemoveEffectFromTarget(int targetID, Dictionary<int, ActiveGameplayEffect> targetEffects)
        {
            if (targetEffects.TryGetValue(targetID, out var activeEffect))
            {
                // We attempt to get the ASC to remove the effect gracefully
                if (_enemyManager.TryGetEnemyASC(targetID, out var targetASC))
                {
                    targetASC.RemoveGameplayEffect(activeEffect);
                }
                targetEffects.Remove(targetID);
            }
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            CleanupAura(asc, spec);
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            CleanupAura(asc, spec);
        }

        private void CleanupAura(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Stop the UniTask cleanly gracefully
            if (_activeSweeps.TryGetValue(spec, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _activeSweeps.Remove(spec);
            }

#if UNITY_EDITOR
            if (_activeGizmos.TryGetValue(spec, out var gizmo))
            {
                if (gizmo != null) UnityEngine.Object.Destroy(gizmo);
                _activeGizmos.Remove(spec);
            }
#endif

            // Remove all leftover active effects
            if (_activeEffects.TryGetValue(spec, out var targetEffects))
            {
                // Copy dictionary keys to list to avoid collection modified exception if RemoveGameplayEffect triggers synchronously
                List<int> keys = new List<int>(targetEffects.Keys);
                foreach (var targetID in keys)
                {
                    RemoveEffectFromTarget(targetID, targetEffects);
                }
                _activeEffects.Remove(spec);
            }
        }
    }


}
