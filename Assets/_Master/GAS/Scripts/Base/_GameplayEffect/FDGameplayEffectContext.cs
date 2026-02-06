using GAS;

namespace FD.Ability
{
    /// <summary>
    /// Farm Defense specific gameplay effect context.
    /// Extends base context with damage type and other FD-specific data.
    /// </summary>
    public class FDGameplayEffectContext : GameplayEffectContext
    {
        /// <summary>
        /// Damage type from the ability (Pierce, Magic, Chaos, etc.)
        /// This is specific to Farm Defense game mechanics.
        /// </summary>
        public EDamageType DamageType { get; set; } = EDamageType.Normal;
        
        
        /// <summary>
        /// Was this a critical hit? (for UI feedback)
        /// </summary>
        public bool IsCriticalHit { get; set; }
        
        /// <summary>
        /// Critical multiplier applied (2x, 3x, etc.)
        /// </summary>
        public float CriticalMultiplier { get; set; } = 1f;
        
        /// <summary>
        /// Type modifier applied (e.g., Magic vs Heavy = 2.0)
        /// </summary>
        public float TypeModifier { get; set; } = 1f;
        
        /// <summary>
        /// Armor reduction percentage (0-1)
        /// </summary>
        public float ArmorReduction { get; set; }
        
        /// <summary>
        /// Helper to get current context as FDGameplayEffectContext
        /// </summary>
        public static new FDGameplayEffectContext Current
        {
            get => GameplayEffectContext.Current as FDGameplayEffectContext;
        }
    }
}