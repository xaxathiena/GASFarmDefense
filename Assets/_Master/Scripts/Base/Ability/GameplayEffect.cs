using System;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// How the modifier calculates its magnitude (similar to Unreal's modifier types)
    /// </summary>
    public enum EModifierCalculationType
    {
        ScalableFloat,          // Fixed value or curve-based scaling
        AttributeBased,         // Based on another attribute (source or target)
        CustomCalculationClass, // Use custom calculation (placeholder for future)
        SetByCaller             // Value set at runtime via GameplayTag
    }
    
    /// <summary>
    /// Source of the attribute for attribute-based calculations
    /// </summary>
    public enum EAttributeSource
    {
        Source,  // From the ability's owner (caster)
        Target   // From the target receiving the effect
    }
    /// <summary>
    /// Attribute type selector for modifiers (can use enum or custom string)
    /// </summary>
    [Serializable]
    public class AttributeSelector
    {
        
        [Tooltip("Select from predefined attributes")]
        public EGameplayAttributeType attributeType = EGameplayAttributeType.Health;
        
        
        /// <summary>
        /// Get the attribute name as string
        /// </summary>
        public EGameplayAttributeType GetAttribute()
        {
            return attributeType;
        }
        
        public AttributeSelector() { }
        
        public AttributeSelector(EGameplayAttributeType type)
        {
            attributeType = type;
        }
    }
    
    /// <summary>
    /// Types of gameplay effect duration
    /// </summary>
    public enum EGameplayEffectDurationType
    {
        Instant,        // Applies once immediately
        Duration,       // Lasts for a specific duration
        Infinite        // Lasts forever until removed
    }
    
    /// <summary>
    /// How the modifier affects the attribute
    /// </summary>
    public enum EGameplayModifierOp
    {
        Add,            // Add to current value
        Multiply,       // Multiply current value
        Divide,         // Divide current value
        Override        // Set to new value
    }
    
    /// <summary>
    /// Defines how an attribute should be modified
    /// </summary>
    [Serializable]
    public class GameplayEffectModifier
    {
        [Header("Target")]
        [Tooltip("Attribute to modify")]
        public AttributeSelector attribute = new AttributeSelector();
        
        [Tooltip("Operation to perform")]
        public EGameplayModifierOp operation = EGameplayModifierOp.Add;
        
        [Header("Magnitude Calculation")]
        [Tooltip("How to calculate the magnitude")]
        public EModifierCalculationType calculationType = EModifierCalculationType.ScalableFloat;
        
        // ScalableFloat calculation
        [Tooltip("Magnitude using ScalableFloat (supports flat, curve, or attribute-based)")]
        public ScalableFloat scalableMagnitude = new ScalableFloat(0f);
        
        // Attribute Based calculation
        [Tooltip("Source attribute for attribute-based calculation")]
        public AttributeSelector backingAttribute = new AttributeSelector();
        
        [Tooltip("Get attribute from Source (caster) or Target (receiver)")]
        public EAttributeSource attributeSource = EAttributeSource.Source;
        
        [Tooltip("Capture attribute when GE is created (true) or when applied (false)")]
        public bool snapshotAttribute = false;
        
        [Tooltip("Coefficient to multiply the attribute value")]
        public float coefficient = 1f;
        
        [Tooltip("Value added before applying coefficient")]
        public float preMultiplyAdditiveValue = 0f;
        
        [Tooltip("Value added after applying coefficient")]
        public float postMultiplyAdditiveValue = 0f;
        
        // SetByCaller
        [Tooltip("GameplayTag for SetByCaller magnitude")]
        public string setByCallerTag = "";
        
        // Legacy support
        [Tooltip("(Deprecated) Simple float magnitude - use scalableMagnitude instead")]
        public float magnitude;
        
        public GameplayEffectModifier() { }
        
        public GameplayEffectModifier(EGameplayAttributeType attrType, EGameplayModifierOp op, float mag)
        {
            attribute = new AttributeSelector(attrType);
            operation = op;
            calculationType = EModifierCalculationType.ScalableFloat;
            scalableMagnitude = new ScalableFloat(mag);
            magnitude = mag; // legacy
        }
        
        /// <summary>
        /// Get the attribute name to modify
        /// </summary>
        public EGameplayAttributeType GetAttributeName()
        {
            return attribute.GetAttribute();
        }
        
        /// <summary>
        /// Calculate the final magnitude value based on calculation type
        /// </summary>
        public float CalculateMagnitude(AbilitySystemComponent sourceASC, AbilitySystemComponent targetASC, float level, float stackCount = 1f)
        {
            float rawMagnitude = 0f;
            
            switch (calculationType)
            {
                case EModifierCalculationType.ScalableFloat:
                    rawMagnitude = scalableMagnitude.GetValueAtLevel(level, sourceASC);
                    break;
                    
                case EModifierCalculationType.AttributeBased:
                    rawMagnitude = CalculateAttributeBasedMagnitude(sourceASC, targetASC);
                    break;
                    
                case EModifierCalculationType.CustomCalculationClass:
                    // Placeholder for future custom calculation classes
                    Debug.LogWarning("Custom Calculation Class not yet implemented");
                    rawMagnitude = 0f;
                    break;
                    
                case EModifierCalculationType.SetByCaller:
                    // Should be set by caller - return 0 if not set
                    // In real implementation, this would check a dictionary on the GameplayEffectSpec
                    Debug.LogWarning($"SetByCaller '{setByCallerTag}' magnitude not implemented in simple apply");
                    rawMagnitude = 0f;
                    break;
            }
            
            return rawMagnitude * stackCount;
        }
        
        /// <summary>
        /// Calculate magnitude based on a backing attribute
        /// </summary>
        private float CalculateAttributeBasedMagnitude(AbilitySystemComponent sourceASC, AbilitySystemComponent targetASC)
        {
            AbilitySystemComponent relevantASC = attributeSource == EAttributeSource.Source ? sourceASC : targetASC;
            
            if (relevantASC == null || relevantASC.AttributeSet == null)
            {
                Debug.LogWarning($"Cannot calculate attribute-based magnitude: {attributeSource} ASC is null");
                return 0f;
            }
            
            var backingAttr = relevantASC.AttributeSet.GetAttribute(backingAttribute.GetAttribute());
            if (backingAttr == null)
            {
                Debug.LogWarning($"Backing attribute '{backingAttribute.GetAttribute()}' not found on {attributeSource}");
                return 0f;
            }
            
            // TODO: Implement snapshotting properly with GameplayEffectSpec
            float attributeValue = snapshotAttribute ? backingAttr.BaseValue : backingAttr.CurrentValue;
            
            // Apply formula: (AttributeValue + PreAdd) * Coefficient + PostAdd
            return (attributeValue + preMultiplyAdditiveValue) * coefficient + postMultiplyAdditiveValue;
        }
    }
    
    /// <summary>
    /// Base class for gameplay effects (buffs, debuffs, damage, healing, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "New Gameplay Effect", menuName = "GAS/Gameplay Effect")]
    public class GameplayEffect : ScriptableObject
    {
        [Header("Effect Info")]
        public string effectName;
        [TextArea(2, 4)]
        public string description;
        
        [Header("Duration")]
        public EGameplayEffectDurationType durationType = EGameplayEffectDurationType.Instant;
        [Tooltip("Duration in seconds (for Duration type)")]
        public float durationMagnitude = 0f;
        
        [Header("Periodic")]
        [Tooltip("Execute effect every X seconds")]
        public bool isPeriodic = false;
        [Tooltip("Period in seconds")]
        public float period = 1f;
        
        [Header("Modifiers")]
        [Tooltip("Attribute modifications this effect applies")]
        public GameplayEffectModifier[] modifiers;
        
        [Header("Gameplay Tags")]
        [Tooltip("Tags granted while effect is active")]
        public string[] grantedTags;
        [Tooltip("Tags added to application requirements (must have ALL)")]
        public string[] applicationRequiredTags;
        [Tooltip("Tags that block this effect (if target has ANY)")]
        public string[] applicationBlockedByTags;
        [Tooltip("Tags to remove on application")]
        public string[] removeTagsOnApplication;
        
        [Header("Stacking")]
        [Tooltip("Can this effect stack?")]
        public bool allowStacking = false;
        [Tooltip("Maximum stacks")]
        public int maxStacks = 1;
        [Tooltip("Does stacking refresh duration?")]
        public bool refreshDurationOnStack = true;
        
        /// <summary>
        /// Check if effect can be applied to target
        /// </summary>
        public bool CanApplyTo(AbilitySystemComponent target)
        {
            // Check required tags
            if (applicationRequiredTags != null && applicationRequiredTags.Length > 0)
            {
                if (!target.HasAllTags(applicationRequiredTags))
                    return false;
            }
            
            // Check blocked tags
            if (applicationBlockedByTags != null && applicationBlockedByTags.Length > 0)
            {
                if (target.HasAnyTags(applicationBlockedByTags))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Apply modifiers to attributes (enhanced with source/target support)
        /// </summary>
        public void ApplyModifiers(AttributeSet targetAttributeSet, AbilitySystemComponent sourceASC = null, AbilitySystemComponent targetASC = null, float level = 1f, float stackCount = 1f)
        {
            if (modifiers == null || targetAttributeSet == null)
                return;
            
            foreach (var modifier in modifiers)
            {
                ApplyModifier(targetAttributeSet, modifier, sourceASC, targetASC, level, stackCount);
            }
        }
        
        /// <summary>
        /// Apply modifiers (legacy - uses simple magnitude)
        /// </summary>
        public void ApplyModifiers(AttributeSet attributeSet, float stackCount = 1f)
        {
            ApplyModifiers(attributeSet, null, null, 1f, stackCount);
        }
        
        /// <summary>
        /// Apply a single modifier to attribute
        /// </summary>
        private void ApplyModifier(AttributeSet targetAttributeSet, GameplayEffectModifier modifier, AbilitySystemComponent sourceASC, AbilitySystemComponent targetASC, float level, float stackCount)
        {
            // Get attribute by name from dictionary
            EGameplayAttributeType attributeType = modifier.GetAttributeName();
            GameplayAttribute targetAttribute = targetAttributeSet.GetAttribute(attributeType);
            
            if (targetAttribute == null)
            {
                Debug.LogWarning($"Attribute '{attributeType}' not found in attribute set!");
                return;
            }
            
            // Calculate magnitude using the new system
            float finalMagnitude = modifier.CalculateMagnitude(sourceASC, targetASC, level, stackCount);
            
            // For backward compatibility, fall back to legacy magnitude if calculation returns 0
            if (finalMagnitude == 0f && modifier.calculationType == EModifierCalculationType.ScalableFloat)
            {
                finalMagnitude = modifier.magnitude * stackCount;
            }
            
            switch (modifier.operation)
            {
                case EGameplayModifierOp.Add:
                    targetAttribute.ModifyCurrentValue(finalMagnitude);
                    break;
                    
                case EGameplayModifierOp.Multiply:
                    float currentValue = targetAttribute.CurrentValue;
                    targetAttribute.SetCurrentValue(currentValue * finalMagnitude);
                    break;
                    
                case EGameplayModifierOp.Divide:
                    if (finalMagnitude != 0f)
                    {
                        float current = targetAttribute.CurrentValue;
                        targetAttribute.SetCurrentValue(current / finalMagnitude);
                    }
                    else
                    {
                        Debug.LogWarning("Attempted to divide attribute by zero!");
                    }
                    break;
                    
                case EGameplayModifierOp.Override:
                    targetAttribute.SetCurrentValue(finalMagnitude);
                    break;
            }
        }
    }
}
