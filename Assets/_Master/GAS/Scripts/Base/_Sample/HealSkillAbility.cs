using UnityEngine;
using GAS;

namespace _Master.Sample
{
    /// <summary>
    /// Heal skill ability - Heals self for 10 HP
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_HealSkill", menuName = "GAS/Sample/Heal Skill Ability")]
    public class HealSkillAbility : GameplayAbility
    {
        [Header("Heal Skill Settings")]
        [Tooltip("Amount to heal")]
        public float healAmount = 10f;
        
        [Tooltip("Heal effect to apply")]
        public GameplayEffect healEffect;
        
        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (healEffect != null)
            {
                // Apply heal effect
                float effectLevel = spec?.Level ?? 1f;
                asc.ApplyGameplayEffectToSelf(healEffect, asc, effectLevel);
                Debug.Log($"{GetAbilityOwner(asc)?.name} used Heal Skill! Restored {healAmount} HP");
            }
            else
            {
                // Direct heal
                var health = asc.AttributeSet.GetAttribute(EGameplayAttributeType.Health);
                if (health != null)
                {
                    health.ModifyCurrentValue(healAmount);
                    Debug.Log($"{GetAbilityOwner(asc)?.name} used Heal Skill! Restored {healAmount} HP");
                }
            }
            
            EndAbility(asc);
        }
    }
}
