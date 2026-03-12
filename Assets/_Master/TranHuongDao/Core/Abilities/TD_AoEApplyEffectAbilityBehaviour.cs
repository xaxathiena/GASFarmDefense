using System.Collections;
using System.Collections.Generic;
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

        // Stores sweeping coroutine references per ability instance
        private readonly Dictionary<GameplayAbilitySpec, Coroutine> _activeCoroutines = new Dictionary<GameplayAbilitySpec, Coroutine>();
        
        // Stores active targets and their effects per ability instance
        private readonly Dictionary<GameplayAbilitySpec, Dictionary<int, ActiveGameplayEffect>> _activeEffects = 
            new Dictionary<GameplayAbilitySpec, Dictionary<int, ActiveGameplayEffect>>();

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
                Object.Instantiate(aoeData.castVfxPrefab, asc.GetOwner().position, Quaternion.identity, asc.GetOwner());
            }

            _activeEffects[spec] = new Dictionary<int, ActiveGameplayEffect>();

            // Start the sweep loop on the Owner's MonoBehaviour (or any globally accessible runner)
            // Assuming asc.GetOwner() has a MonoBehaviour, otherwise we might need a generic CoroutineRunner.
            var ownerMono = asc.GetOwner().GetComponent<MonoBehaviour>();
            if (ownerMono != null)
            {
                var routine = ownerMono.StartCoroutine(AuraSweepRoutine(aoeData, asc, spec));
                _activeCoroutines[spec] = routine;
            }
            else
            {
                Debug.LogWarning($"[TD_AoEApplyEffect] Cannot start Aura sweep coroutine because {asc.GetOwner().name} has no MonoBehaviour.");
            }
        }

        private IEnumerator AuraSweepRoutine(TD_AoEApplyEffectAbilityData aoeData, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var targetEffects = _activeEffects[spec];
            List<int> sweepCache = new List<int>(32);
            List<int> toRemove = new List<int>(32);

            while (spec.IsActive && asc.GetOwner() != null)
            {
                Vector3 originPos = asc.GetOwner().position;
                sweepCache.Clear();

                // Depending on the target type, gather the IDs.
                // Assuming IEnemyManager provides an Ally manager equivalence or we just sweep enemies for now.
                // For a fully fleshed out system, you might have IUnitManager that can get both.
                if (aoeData.targetType == EAuraTargetType.Enemies || aoeData.targetType == EAuraTargetType.Both)
                {
                    _enemyManager.GetEnemiesInRange(originPos, aoeData.captureRadius, sweepCache);
                }
                
                // Note: If Allies or Both, you would ideally add allies to sweepCache here. 
                // E.g., _allyManager.GetAlliesInRange(...)

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

                yield return new WaitForSeconds(aoeData.auraUpdateRate);
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
            // Stop coroutine
            if (_activeCoroutines.TryGetValue(spec, out var routine))
            {
                if (asc.GetOwner() != null)
                {
                    var ownerMono = asc.GetOwner().GetComponent<MonoBehaviour>();
                    if (ownerMono != null && routine != null)
                    {
                        ownerMono.StopCoroutine(routine);
                    }
                }
                _activeCoroutines.Remove(spec);
            }

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
