using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Alternative approach using Reflection to find attributes
    /// Use this if you want even more flexibility without registering attributes
    /// </summary>
    public static class AttributeReflectionHelper
    {
        /// <summary>
        /// Get attribute by name using reflection
        /// This allows accessing properties without hardcoding
        /// </summary>
        public static GameplayAttribute GetAttributeByReflection(AttributeSet attributeSet, string attributeName)
        {
            if (attributeSet == null || string.IsNullOrEmpty(attributeName))
                return null;
            
            // Try to get property
            var propertyInfo = attributeSet.GetType().GetProperty(attributeName);
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(GameplayAttribute))
            {
                return propertyInfo.GetValue(attributeSet) as GameplayAttribute;
            }
            
            // Try to get field
            var fieldInfo = attributeSet.GetType().GetField(attributeName);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(GameplayAttribute))
            {
                return fieldInfo.GetValue(attributeSet) as GameplayAttribute;
            }
            
            return null;
        }
        
        /// <summary>
        /// Apply modifier using reflection (fallback method)
        /// </summary>
        public static bool ApplyModifierWithReflection(AttributeSet attributeSet, string attributeName, EGameplayModifierOp operation, float magnitude)
        {
            var attribute = GetAttributeByReflection(attributeSet, attributeName);
            
            if (attribute == null)
            {
                //Debug.LogWarning($"Attribute '{attributeName}' not found via reflection!");
                return false;
            }
            
            switch (operation)
            {
                case EGameplayModifierOp.Add:
                    attribute.ModifyCurrentValue(magnitude);
                    break;
                    
                case EGameplayModifierOp.Multiply:
                    attribute.SetCurrentValue(attribute.CurrentValue * magnitude);
                    break;
                    
                case EGameplayModifierOp.Override:
                    attribute.SetCurrentValue(magnitude);
                    break;
            }
            
            return true;
        }
    }
}
