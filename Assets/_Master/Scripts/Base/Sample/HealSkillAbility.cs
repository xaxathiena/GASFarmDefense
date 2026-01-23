using UnityEngine;
using _Master.Base.Ability;

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
        
        protected override void OnAbilityActivated()
        {
            if (healEffect != null)
            {
                // Apply heal effect
                ownerASC.ApplyGameplayEffectToSelf(healEffect, ownerASC);
                Debug.Log($"{owner.name} used Heal Skill! Restored {healAmount} HP");
            }
            else
            {
                // Direct heal
                var health = ownerASC.AttributeSet.GetAttribute(EGameplayAttributeType.Health);
                if (health != null)
                {
                    health.ModifyCurrentValue(healAmount);
                    Debug.Log($"{owner.name} used Heal Skill! Restored {healAmount} HP");
                }
            }
            
            EndAbility();
        }
    }
}
