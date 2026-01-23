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
        [SerializeField] private float currentValue;
        
        public float CurrentValue
        {
            get => currentValue;
            set => currentValue = value;
        }
        
        public event Action<float, float> OnValueChanged; // oldValue, newValue
        
        public GameplayAttribute(float initialValue = 0f)
        {
            currentValue = initialValue;
        }
        
        /// <summary>
        /// Modify the current value
        /// </summary>
        public void ModifyCurrentValue(float delta)
        {
            float oldValue = currentValue;
            currentValue = currentValue + delta;
            
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
            
            if (!Mathf.Approximately(oldValue, currentValue))
            {
                OnValueChanged?.Invoke(oldValue, currentValue);
            }
        }
    }
}
