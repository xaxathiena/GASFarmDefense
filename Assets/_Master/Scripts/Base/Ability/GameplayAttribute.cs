using System;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Represents a gameplay attribute (Health, Mana, Stamina, etc.)
    /// Similar to Unreal's FGameplayAttributeData with BaseValue and CurrentValue
    /// </summary>
    [Serializable]
    public class GameplayAttribute
    {
        [SerializeField] private float baseValue;
        [SerializeField] private float currentValue;
        [SerializeField] private float maxValue = float.MaxValue;
        [SerializeField] private bool hasMaxValue = false;
        
        /// <summary>
        /// The base permanent value of the attribute (unaffected by temporary modifiers)
        /// </summary>
        public float BaseValue
        {
            get => baseValue;
            set
            {
                float oldBase = baseValue;
                baseValue = value;
                
                // Update current value to match if no modifiers are active
                if (Mathf.Approximately(currentValue, oldBase))
                {
                    currentValue = baseValue;
                }
                
                if (!Mathf.Approximately(oldBase, baseValue))
                {
                    OnBaseValueChanged?.Invoke(oldBase, baseValue);
                }
            }
        }
        
        /// <summary>
        /// The current value including temporary modifiers
        /// </summary>
        public float CurrentValue
        {
            get => currentValue;
            set => SetCurrentValue(value);
        }
        
        /// <summary>
        /// Maximum allowed value for this attribute (if hasMaxValue is true)
        /// </summary>
        public float MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }
        
        /// <summary>
        /// Whether this attribute has a maximum value limit
        /// </summary>
        public bool HasMaxValue
        {
            get => hasMaxValue;
            set => hasMaxValue = value;
        }
        
        public event Action<float, float> OnValueChanged; // oldValue, newValue
        public event Action<float, float> OnBaseValueChanged; // oldBase, newBase
        
        public GameplayAttribute(float initialValue = 0f, float maxVal = float.MaxValue, bool hasMax = false)
        {
            baseValue = initialValue;
            currentValue = initialValue;
            maxValue = maxVal;
            hasMaxValue = hasMax;
        }
        
        /// <summary>
        /// Modify the current value by a delta amount
        /// </summary>
        public void ModifyCurrentValue(float delta)
        {
            float oldValue = currentValue;
            currentValue = currentValue + delta;
            
            // Clamp to max value if applicable
            if (hasMaxValue && currentValue > maxValue)
            {
                currentValue = maxValue;
            }
            
            // Clamp to minimum 0 for most attributes
            if (currentValue < 0f)
            {
                currentValue = 0f;
            }
            
            if (!Mathf.Approximately(oldValue, currentValue))
            {
                OnValueChanged?.Invoke(oldValue, currentValue);
            }
        }
        
        /// <summary>
        /// Set current value directly
        /// </summary>
        public void SetCurrentValue(float value)
        {
            float oldValue = currentValue;
            currentValue = value;
            
            // Clamp to max value if applicable
            if (hasMaxValue && currentValue > maxValue)
            {
                currentValue = maxValue;
            }
            
            // Clamp to minimum 0 for most attributes
            if (currentValue < 0f)
            {
                currentValue = 0f;
            }
            
            if (!Mathf.Approximately(oldValue, currentValue))
            {
                OnValueChanged?.Invoke(oldValue, currentValue);
            }
        }
        
        /// <summary>
        /// Modify the base value (permanent change)
        /// </summary>
        public void ModifyBaseValue(float delta)
        {
            float oldBase = baseValue;
            baseValue += delta;
            
            // Also update current value by the same delta
            float oldCurrent = currentValue;
            currentValue += delta;
            
            if (hasMaxValue && currentValue > maxValue)
            {
                currentValue = maxValue;
            }
            
            if (currentValue < 0f)
            {
                currentValue = 0f;
            }
            
            if (!Mathf.Approximately(oldBase, baseValue))
            {
                OnBaseValueChanged?.Invoke(oldBase, baseValue);
            }
            
            if (!Mathf.Approximately(oldCurrent, currentValue))
            {
                OnValueChanged?.Invoke(oldCurrent, currentValue);
            }
        }
        
        /// <summary>
        /// Set the base value directly (permanent change)
        /// </summary>
        public void SetBaseValue(float value)
        {
            BaseValue = value;
        }
    }
}
