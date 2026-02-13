using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Service for applying GameplayEffects to targets.
    /// Handles validation, modifier application, and aggregation.
    /// Injectable via VContainer as Singleton.
    /// </summary>
    public class GameplayEffectService
    {
        private readonly GameplayEffectCalculationService calculationService;
        private readonly IDebugService debug;
        
        public GameplayEffectService(
            GameplayEffectCalculationService calculationService,
            IDebugService debug)
        {
            this.calculationService = calculationService;
            this.debug = debug;
        }
        
        /// <summary>
        /// Check if effect can be applied to target based on tags.
        /// </summary>
        public bool CanApplyTo(GameplayEffect effectData, AbilitySystemComponent target)
        {
            // Check required tags (must have ALL)
            if (effectData.applicationRequiredTags != null && effectData.applicationRequiredTags.Length > 0)
            {
                if (!target.HasAllTags(effectData.applicationRequiredTags))
                {
                    return false;
                }
            }
            
            // Check blocked tags (if ANY present, block)
            if (effectData.applicationBlockedByTags != null && effectData.applicationBlockedByTags.Length > 0)
            {
                if (target.HasAnyTags(effectData.applicationBlockedByTags))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Apply a modifier with proper aggregation system.
        /// </summary>
        /// <param name="effectData">GameplayEffect ScriptableObject data</param>
        /// <param name="modifier">Modifier to apply</param>
        /// <param name="sourceASC">Source AbilitySystemComponent</param>
        /// <param name="targetASC">Target AbilitySystemComponent</param>
        /// <param name="level">Effect level</param>
        /// <param name="stackCount">Stack count</param>
        /// <param name="activeEffect">Runtime effect instance (null for instant effects)</param>
        /// <param name="isInstant">True for instant effects, false for duration/infinite</param>
        /// <param name="context">Gameplay effect context</param>
        public void ApplyModifierWithAggregation(
            GameplayEffect effectData,
            GameplayEffectModifier modifier,
            AbilitySystemComponent sourceASC,
            AbilitySystemComponent targetASC,
            float level,
            float stackCount,
            ActiveGameplayEffect activeEffect,
            bool isInstant,
            GameplayEffectContext context)
        {
            if (targetASC == null || targetASC.AttributeSet == null)
            {
                Debug.LogWarning("[GameplayEffectService] Target ASC or AttributeSet is null!");
                return;
            }
            
            // Get target attribute
            EGameplayAttributeType attributeType = modifier.GetAttributeName();
            GameplayAttribute targetAttribute = targetASC.AttributeSet.GetAttribute(attributeType);
            
            if (targetAttribute == null)
            {
                Debug.LogWarning($"[GameplayEffectService] Attribute '{attributeType}' not found in target's attribute set!");
                return;
            }
            
            // Calculate magnitude using service
            float finalMagnitude = calculationService.CalculateMagnitude(
                modifier,
                sourceASC,
                targetASC,
                level,
                stackCount,
                context);
            
            // Apply based on effect type
            if (isInstant)
            {
                // Instant effects: Directly modify CurrentValue (not tracked)
                switch (modifier.operation)
                {
                    case EGameplayModifierOp.Add:
                        targetAttribute.ModifyCurrentValue(finalMagnitude);
                        break;
                        
                    case EGameplayModifierOp.Multiply:
                        targetAttribute.ModifyCurrentValue(targetAttribute.CurrentValue * finalMagnitude);
                        break;
                        
                    case EGameplayModifierOp.Divide:
                        if (finalMagnitude != 0)
                            targetAttribute.ModifyCurrentValue(targetAttribute.CurrentValue / finalMagnitude);
                        break;
                        
                    case EGameplayModifierOp.Override:
                        targetAttribute.SetCurrentValue(finalMagnitude);
                        break;
                }
                
                debug.Log($"[GE] Instant {modifier.operation} {finalMagnitude} to {attributeType} → {targetAttribute.CurrentValue}");
            }
            else
            {
                // Duration/Infinite effects: Use aggregator system (tracked, removable)
                targetAttribute.AddModifier(activeEffect, modifier.operation, finalMagnitude);
                
                debug.Log($"[GE] Duration {modifier.operation} {finalMagnitude} to {attributeType} (tracked)");
            }
        }
        
        /// <summary>
        /// Apply all modifiers from a GameplayEffect.
        /// </summary>
        public void ApplyModifiers(
            GameplayEffect effectData,
            AbilitySystemComponent sourceASC,
            AbilitySystemComponent targetASC,
            float level,
            float stackCount,
            ActiveGameplayEffect activeEffect,
            GameplayEffectContext context)
        {
            if (effectData.modifiers == null || targetASC == null || targetASC.AttributeSet == null)
            {
                return;
            }
            
            bool isInstant = effectData.durationType == EGameplayEffectDurationType.Instant;
            
            foreach (var modifier in effectData.modifiers)
            {
                ApplyModifierWithAggregation(
                    effectData,
                    modifier,
                    sourceASC,
                    targetASC,
                    level,
                    stackCount,
                    activeEffect,
                    isInstant,
                    context);
            }
        }
    }
}
