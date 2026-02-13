using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Service for calculating GameplayEffect modifier magnitudes.
    /// Stateless, injectable via VContainer.
    /// Extracted from GameplayEffectModifier.CalculateMagnitude logic.
    /// </summary>
    public class GameplayEffectCalculationService
    {
        /// <summary>
        /// Calculate the final magnitude value for a modifier.
        /// </summary>
        /// <param name="modifier">Modifier data from ScriptableObject</param>
        /// <param name="sourceASC">Source AbilitySystemComponent (caster)</param>
        /// <param name="targetASC">Target AbilitySystemComponent (receiver)</param>
        /// <param name="level">Effect level for scaling</param>
        /// <param name="stackCount">Number of stacks</param>
        /// <param name="context">Gameplay effect context (replaces static Current)</param>
        /// <returns>Final calculated magnitude</returns>
        public float CalculateMagnitude(
            GameplayEffectModifier modifier,
            AbilitySystemComponent sourceASC,
            AbilitySystemComponent targetASC,
            float level,
            float stackCount,
            GameplayEffectContext context)
        {
            float rawMagnitude = 0f;
            
            switch (modifier.calculationType)
            {
                case EModifierCalculationType.ScalableFloat:
                    rawMagnitude = modifier.scalableMagnitude.GetValueAtLevel(level, sourceASC);
                    break;
                    
                case EModifierCalculationType.AttributeBased:
                    rawMagnitude = CalculateAttributeBasedMagnitude(modifier, sourceASC, targetASC);
                    break;
                    
                case EModifierCalculationType.CustomCalculationClass:
                    rawMagnitude = CalculateCustomMagnitude(modifier, sourceASC, targetASC, level, context);
                    break;
                    
                case EModifierCalculationType.SetByCaller:
                    // Should be set by caller via GameplayEffectSpec
                    Debug.LogWarning($"SetByCaller '{modifier.setByCallerTag}' magnitude not yet implemented");
                    rawMagnitude = 0f;
                    break;
            }
            
            return rawMagnitude * stackCount;
        }
        
        /// <summary>
        /// Calculate magnitude based on a backing attribute.
        /// Formula: ((BackingAttribute + PreAdd) * Coefficient) + PostAdd
        /// </summary>
        private float CalculateAttributeBasedMagnitude(
            GameplayEffectModifier modifier,
            AbilitySystemComponent sourceASC,
            AbilitySystemComponent targetASC)
        {
            AbilitySystemComponent relevantASC = modifier.attributeSource == EAttributeSource.Source 
                ? sourceASC 
                : targetASC;
            
            if (relevantASC == null || relevantASC.AttributeSet == null)
            {
                Debug.LogWarning($"Cannot calculate attribute-based magnitude: {modifier.attributeSource} ASC is null");
                return 0f;
            }
            
            var backingAttr = relevantASC.AttributeSet.GetAttribute(modifier.backingAttribute.GetAttribute());
            if (backingAttr == null)
            {
                Debug.LogWarning($"Backing attribute {modifier.backingAttribute.GetAttribute()} not found on {modifier.attributeSource}");
                return 0f;
            }
            
            // Use snapshot or current value
            float attributeValue = modifier.snapshotAttribute ? backingAttr.BaseValue : backingAttr.CurrentValue;
            
            // Apply formula: ((attribute + preAdd) * coefficient) + postAdd
            float magnitude = ((attributeValue + modifier.preMultiplyAdditiveValue) * modifier.coefficient) 
                            + modifier.postMultiplyAdditiveValue;
            
            return magnitude;
        }
        
        /// <summary>
        /// Calculate magnitude using custom calculation class.
        /// </summary>
        private float CalculateCustomMagnitude(
            GameplayEffectModifier modifier,
            AbilitySystemComponent sourceASC,
            AbilitySystemComponent targetASC,
            float level,
            GameplayEffectContext context)
        {
            if (modifier.customCalculation == null)
            {
                Debug.LogWarning("CustomCalculationClass selected but no calculation assigned!");
                return 0f;
            }
            
            if (context == null)
            {
                Debug.LogWarning("[CustomCalculation] No context provided! Using base magnitude.");
                return modifier.scalableMagnitude.GetValueAtLevel(level, sourceASC);
            }
            
            // Get base magnitude from scalableMagnitude
            float baseMagnitude = modifier.scalableMagnitude.GetValueAtLevel(level, sourceASC);
            
            // Calculate using custom class
            return modifier.customCalculation.CalculateMagnitude(
                context,
                sourceASC,
                targetASC,
                baseMagnitude,
                level
            );
        }
    }
}
