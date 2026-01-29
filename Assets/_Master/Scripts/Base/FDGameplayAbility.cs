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
        
        [Tooltip("Base damage of this ability")]
        public ScalableFloat baseDamage = new ScalableFloat();
        
        [Header("Effect")]
        [Tooltip("GameplayEffect to apply when ability activates")]
        public GameplayEffect effectToApply;
        
        /// <summary>
        /// Create FD-specific context for this ability
        /// </summary>
        protected virtual FDGameplayEffectContext CreateFDContext(AbilitySystemComponent source, AbilitySystemComponent target, GameplayAbilitySpec spec)
        {
            float level = GetAbilityLevel(spec);
            
            var context = new FDGameplayEffectContext
            {
                SourceASC = source,
                TargetASC = target,
                SourceAbility = this,
                Level = level,
                DamageType = damageType,
                BaseDamage = baseDamage.GetValueAtLevel(level, source)
            };
            
            return context;
        }
        
        /// <summary>
        /// Apply effect with FD context
        /// </summary>
        protected void ApplyEffectWithContext(GameplayEffect effect, AbilitySystemComponent source, AbilitySystemComponent target, GameplayAbilitySpec spec)
        {
            if (effect == null)
            {
                Debug.LogWarning($"[{abilityName}] No effect to apply!");
                return;
            }
            
            // Create FD context
            var context = CreateFDContext(source, target, spec);
            
            // Set as current context for calculation pipeline
            context.MakeCurrent();
            
            try
            {
                // Apply effect with context
                effect.ApplyModifiers(
                    target.AttributeSet,
                    source,
                    target,
                    context.Level,
                    context.StackCount
                );
            }
            finally
            {
                // Always clear context
                GameplayEffectContext.ClearCurrent();
            }
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