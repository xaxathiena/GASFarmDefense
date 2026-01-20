using System;
using UnityEngine;

namespace _Master.Base.Ability
{
    /// <summary>
    /// Represents a gameplay attribute (Health, Mana, Stamina, etc.)
    /// </summary>
    [Serializable]
    public class GameplayAttribute
    {
        [SerializeField] private float baseValue;
        [SerializeField] private float currentValue;
        
        private float minValue = 0f;
        private float maxValue = float.MaxValue;
        
        public float BaseValue
        {
            get => baseValue;
            set
            {
                baseValue = value;
                currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
            }
        }
        
        public float CurrentValue
        {
            get => currentValue;
            set => currentValue = Mathf.Clamp(value, minValue, maxValue);
        }
        
        public float MinValue
        {
            get => minValue;
            set => minValue = value;
        }
        
        public float MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }
        
        public event Action<float, float> OnValueChanged; // oldValue, newValue
        
        public GameplayAttribute(float initialValue = 0f)
        {
            baseValue = initialValue;
            currentValue = initialValue;
        }
        
        public GameplayAttribute(float initialValue, float min, float max)
        {
            minValue = min;
            maxValue = max;
            baseValue = Mathf.Clamp(initialValue, min, max);
            currentValue = baseValue;
        }
        
        /// <summary>
        /// Modify the current value
        /// </summary>
        public void ModifyCurrentValue(float delta)
        {
            float oldValue = currentValue;
            currentValue = Mathf.Clamp(currentValue + delta, minValue, maxValue);
            
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
            currentValue = Mathf.Clamp(value, minValue, maxValue);
            
            if (!Mathf.Approximately(oldValue, currentValue))
            {
                OnValueChanged?.Invoke(oldValue, currentValue);
            }
        }
        
        /// <summary>
        /// Get percentage of current/max value
        /// </summary>
        public float GetPercentage()
        {
            if (maxValue == 0)
                return 0f;
            return currentValue / maxValue;
        }
        
        /// <summary>
        /// Reset to base value
        /// </summary>
        public void ResetToBase()
        {
            SetCurrentValue(baseValue);
        }
    }
}
