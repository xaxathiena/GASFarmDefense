using UnityEngine;

namespace _Master.Base.Ability
{
    /// <summary>
    /// Example ability that applies a gameplay effect
    /// </summary>
    [CreateAssetMenu(fileName = "New Effect Ability", menuName = "GAS/Abilities/Effect Ability")]
    public class GameplayEffectAbility : GameplayAbility
    {
        [Header("Gameplay Effect Settings")]
        [Tooltip("The gameplay effect to apply")]
        public GameplayEffect effectToApply;
        
        [Tooltip("Apply to self or target")]
        public bool applyToSelf = true;
        
        [Tooltip("Range to find target (if not self)")]
        public float targetRange = 10f;
        
        [Tooltip("Layers to target")]
        public LayerMask targetLayers;
        
        protected override void OnAbilityActivated()
        {
            if (effectToApply == null)
            {
                Debug.LogWarning($"{abilityName} has no effect to apply!");
                EndAbility();
                return;
            }
            
            if (applyToSelf)
            {
                // Apply to self
                ownerASC.ApplyGameplayEffectToSelf(effectToApply, ownerASC);
                Debug.Log($"{abilityName} applied {effectToApply.effectName} to self");
            }
            else
            {
                // Find target and apply
                Collider[] targets = Physics.OverlapSphere(owner.transform.position, targetRange, targetLayers);
                
                foreach (var targetCollider in targets)
                {
                    var targetASC = targetCollider.GetComponent<AbilitySystemComponent>();
                    if (targetASC != null && targetASC != ownerASC)
                    {
                        ownerASC.ApplyGameplayEffectToTarget(effectToApply, targetASC, ownerASC);
                        Debug.Log($"{abilityName} applied {effectToApply.effectName} to {targetASC.gameObject.name}");
                    }
                }
            }
            
            // End ability immediately
            EndAbility();
        }
    }
}
