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
        
        // Custom Calculation Class
        [Header("Custom Calculation (Optional)")]
        [Tooltip("Custom calculation class (e.g., WC3DamageCalculation for FD game)")]
        public FD.Ability.DamageCalculationBase customCalculation;
        
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
    }
    
    /// <summary>
    /// Base class for gameplay effects (buffs, debuffs, damage, healing, etc.)
    /// PURE DATA ONLY - All logic extracted to GameplayEffectService and GameplayEffectCalculationService.
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
        public GameplayTag[] grantedTags;
        [Tooltip("Tags added to application requirements (must have ALL)")]
        public GameplayTag[] applicationRequiredTags;
        [Tooltip("Tags that block this effect (if target has ANY)")]
        public GameplayTag[] applicationBlockedByTags;
        [Tooltip("Tags to remove on application")]
        public GameplayTag[] removeTagsOnApplication;
        
        [Header("Stacking")]
        [Tooltip("Can this effect stack?")]
        public bool allowStacking = false;
        [Tooltip("Maximum stacks")]
        public int maxStacks = 1;
        [Tooltip("Does stacking refresh duration?")]
        public bool refreshDurationOnStack = true;
    }
}
