using GAS;
using UnityEngine;

namespace FD.Ability
{
    /// <summary>
    /// Farm Defense specific gameplay ability.
    /// Extends base GameplayAbility with FD-specific properties like damage type.
    /// </summary>
    [CreateAssetMenu(fileName = "FDGameplayAbility", menuName = "FD/Abilities/FD Gameplay Ability")]
    public class FDGameplayAbility : GameplayAbility
    {
        [Header("FD Damage Configuration")]
        [Tooltip("Damage type of this ability (Pierce, Magic, Chaos, etc.)")]
        public EDamageType damageType = EDamageType.Normal;
        
        /// <summary>
        /// Create FD-specific context for this ability
        /// </summary>
        protected override GameplayEffectContext CreateFDContext(AbilitySystemComponent source, AbilitySystemComponent target, GameplayAbilitySpec spec)
        {
            float level = GetAbilityLevel(spec);
            
            var context = new FDGameplayEffectContext
            {
                SourceASC = source,
                TargetASC = target,
                SourceAbility = this,
                Level = level,
                DamageType = damageType,
            };
            
            return context;
        }
        
        
        /// <summary>
        /// Override to use FD context when applying effects
        /// </summary>
        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            base.OnAbilityActivated(asc, spec);
            
            // Default implementation - subclasses can override
            // For example: ProjectileAbility will handle targeting differently
        }
    }
}