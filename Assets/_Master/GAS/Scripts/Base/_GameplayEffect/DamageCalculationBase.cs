using GAS;
using UnityEngine;

namespace FD.Ability
{
    /// <summary>
    /// Base class for custom damage calculations.
    /// Games can extend this to implement their own damage formulas.
    /// Receives generic GameplayEffectContext that can be cast to game-specific context.
    /// </summary>
    public abstract class DamageCalculationBase : ScriptableObject
    {
        /// <summary>
        /// Calculate final damage/magnitude value.
        /// </summary>
        /// <param name="context">Generic gameplay effect context (cast to your game's context type)</param>
        /// <param name="sourceASC">Source ability system component (attacker)</param>
        /// <param name="targetASC">Target ability system component (defender)</param>
        /// <param name="baseMagnitude">Base magnitude from modifier (can be damage, healing, etc.)</param>
        /// <param name="level">Effect/ability level</param>
        /// <returns>Final calculated magnitude</returns>
        public abstract float CalculateMagnitude(
            GameplayEffectContext context,
            AbilitySystemComponent sourceASC,
            AbilitySystemComponent targetASC,
            float baseMagnitude,
            float level
        );
    }
}
