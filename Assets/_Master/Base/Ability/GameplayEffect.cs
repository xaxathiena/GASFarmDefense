using System;
using UnityEngine;

namespace _Master.Base.Ability
{
    /// <summary>
    /// Attribute type selector for modifiers (can use enum or custom string)
    /// </summary>
    [Serializable]
    public class AttributeSelector
    {
        [Tooltip("Use predefined attribute type")]
        public bool useEnum = true;
        
        [Tooltip("Select from predefined attributes")]
        public EGameplayAttributeType attributeType = EGameplayAttributeType.Health;
        
        [Tooltip("Or use custom attribute name (when useEnum is false)")]
        public string customAttributeName = "";
        
        /// <summary>
        /// Get the attribute name as string
        /// </summary>
        public string GetAttributeName()
        {
            return useEnum ? attributeType.ToString() : customAttributeName;
        }
        
        public AttributeSelector() { }
        
        public AttributeSelector(EGameplayAttributeType type)
        {
            useEnum = true;
            attributeType = type;
        }
        
        public AttributeSelector(string customName)
        {
            useEnum = false;
            customAttributeName = customName;
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
        Override        // Set to new value
    }
    
    /// <summary>
    /// Defines how an attribute should be modified
    /// </summary>
    [Serializable]
    public class GameplayEffectModifier
    {
        [Tooltip("Attribute to modify")]
        public AttributeSelector attribute = new AttributeSelector();
        
        [Tooltip("Operation to perform")]
        public EGameplayModifierOp operation = EGameplayModifierOp.Add;
        
        [Tooltip("Magnitude of the modification")]
        public float magnitude;
        
        public GameplayEffectModifier() { }
        
        public GameplayEffectModifier(EGameplayAttributeType attrType, EGameplayModifierOp op, float mag)
        {
            attribute = new AttributeSelector(attrType);
            operation = op;
            magnitude = mag;
        }
        
        public GameplayEffectModifier(string attrName, EGameplayModifierOp op, float mag)
        {
            attribute = new AttributeSelector(attrName);
            operation = op;
            magnitude = mag;
        }
        
        /// <summary>
        /// Get the attribute name
        /// </summary>
        public string GetAttributeName()
        {
            return attribute.GetAttributeName();
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
        /// Apply modifiers to attributes (generic, works with any AttributeSet)
        /// </summary>
        public void ApplyModifiers(AttributeSet attributeSet, float stackCount = 1f)
        {
            if (modifiers == null || attributeSet == null)
                return;
            
            foreach (var modifier in modifiers)
            {
                ApplyModifier(attributeSet, modifier, stackCount);
            }
        }
        
        /// <summary>
        /// Apply a single modifier to attribute
        /// </summary>
        private void ApplyModifier(AttributeSet attributeSet, GameplayEffectModifier modifier, float stackCount)
        {
            // Get attribute by name from dictionary
            string attributeName = modifier.GetAttributeName();
            GameplayAttribute targetAttribute = attributeSet.GetAttribute(attributeName);
            
            if (targetAttribute == null)
            {
                Debug.LogWarning($"Attribute '{attributeName}' not found in attribute set!");
                return;
            }
            
            float finalMagnitude = modifier.magnitude * stackCount;
            
            switch (modifier.operation)
            {
                case EGameplayModifierOp.Add:
                    targetAttribute.ModifyCurrentValue(finalMagnitude);
                    break;
                    
                case EGameplayModifierOp.Multiply:
                    float currentValue = targetAttribute.CurrentValue;
                    targetAttribute.SetCurrentValue(currentValue * finalMagnitude);
                    break;
                    
                case EGameplayModifierOp.Override:
                    targetAttribute.SetCurrentValue(finalMagnitude);
                    break;
            }
        }
    }
}
