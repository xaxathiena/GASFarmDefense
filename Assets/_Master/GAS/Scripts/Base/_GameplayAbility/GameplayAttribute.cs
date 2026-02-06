using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Modifier entry for aggregation system
    /// </summary>
    public class AttributeModifier
    {
        public ActiveGameplayEffect SourceEffect { get; set; }
        public EGameplayModifierOp Operation { get; set; }
        public float Magnitude { get; set; }
        public float ApplyTime { get; set; }

        public AttributeModifier(ActiveGameplayEffect sourceEffect, EGameplayModifierOp operation, float magnitude)
        {
            SourceEffect = sourceEffect;
            Operation = operation;
            Magnitude = magnitude;
            ApplyTime = Time.time;
        }
    }

    /// <summary>
    /// Aggregates modifiers to calculate final attribute value
    /// Execution order: Base → Add → Multiply → Divide
    /// </summary>
    public class AttributeModifierAggregator
    {
        private List<AttributeModifier> modifiers = new List<AttributeModifier>();

        /// <summary>
        /// Add a modifier to the aggregator
        /// </summary>
        public void AddModifier(ActiveGameplayEffect sourceEffect, EGameplayModifierOp operation, float magnitude)
        {
            modifiers.Add(new AttributeModifier(sourceEffect, operation, magnitude));
        }

        /// <summary>
        /// Remove all modifiers from a specific effect
        /// </summary>
        public void RemoveModifiersFromEffect(ActiveGameplayEffect sourceEffect)
        {
            modifiers.RemoveAll(m => m.SourceEffect == sourceEffect);
        }

        /// <summary>
        /// Calculate final value from base value and all modifiers
        /// Execution order: Base → Add → Multiply → Divide → Override (last override wins)
        /// </summary>
        public float CalculateFinalValue(float baseValue)
        {
            float result = baseValue;

            // Check for Override operations (last one wins)
            var overrides = modifiers.Where(m => m.Operation == EGameplayModifierOp.Override)
                                     .OrderBy(m => m.ApplyTime)
                                     .ToList();
            if (overrides.Count > 0)
            {
                return overrides.Last().Magnitude;
            }

            // Add operations
            foreach (var mod in modifiers.Where(m => m.Operation == EGameplayModifierOp.Add))
            {
                result += mod.Magnitude;
            }

            // Multiply operations
            foreach (var mod in modifiers.Where(m => m.Operation == EGameplayModifierOp.Multiply))
            {
                result *= mod.Magnitude;
            }

            // Divide operations
            foreach (var mod in modifiers.Where(m => m.Operation == EGameplayModifierOp.Divide))
            {
                if (mod.Magnitude != 0f)
                {
                    result /= mod.Magnitude;
                }
            }

            return result;
        }

        /// <summary>
        /// Get all modifiers (for debugging)
        /// </summary>
        public List<AttributeModifier> GetModifiers()
        {
            return new List<AttributeModifier>(modifiers);
        }

        /// <summary>
        /// Clear all modifiers
        /// </summary>
        public void Clear()
        {
            modifiers.Clear();
        }
    }

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

        // Modifier aggregation system
        [NonSerialized] private AttributeModifierAggregator aggregator = new AttributeModifierAggregator();
        [NonSerialized] private bool isDirty = false;
        
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
            get
            {
                if (isDirty)
                {
                    RecalculateCurrentValue();
                }
                return currentValue;
            }
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
            aggregator = new AttributeModifierAggregator();
            isDirty = false;
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

        #region Modifier Aggregation System

        /// <summary>
        /// Add a modifier to this attribute (from Duration/Infinite effects)
        /// </summary>
        public void AddModifier(ActiveGameplayEffect sourceEffect, EGameplayModifierOp operation, float magnitude)
        {
            if (aggregator == null)
            {
                aggregator = new AttributeModifierAggregator();
            }

            aggregator.AddModifier(sourceEffect, operation, magnitude);
            isDirty = true;

            // Recalculate immediately
            RecalculateCurrentValue();
        }

        /// <summary>
        /// Remove all modifiers from a specific effect
        /// </summary>
        public void RemoveModifiersFromEffect(ActiveGameplayEffect sourceEffect)
        {
            if (aggregator == null)
            {
                return;
            }

            aggregator.RemoveModifiersFromEffect(sourceEffect);
            isDirty = true;

            // Recalculate immediately
            RecalculateCurrentValue();
        }

        /// <summary>
        /// Recalculate current value from base value and all active modifiers
        /// </summary>
        public void RecalculateCurrentValue()
        {
            if (aggregator == null)
            {
                aggregator = new AttributeModifierAggregator();
            }

            float oldValue = currentValue;
            float newValue = aggregator.CalculateFinalValue(baseValue);

            // Apply clamping
            if (hasMaxValue && newValue > maxValue)
            {
                newValue = maxValue;
            }

            if (newValue < 0f)
            {
                newValue = 0f;
            }

            currentValue = newValue;
            isDirty = false;

            // Fire event if changed
            if (!Mathf.Approximately(oldValue, currentValue))
            {
                OnValueChanged?.Invoke(oldValue, currentValue);
            }
        }

        /// <summary>
        /// Get active modifiers (for debugging)
        /// </summary>
        public List<AttributeModifier> GetActiveModifiers()
        {
            if (aggregator == null)
            {
                return new List<AttributeModifier>();
            }
            return aggregator.GetModifiers();
        }

        #endregion
    }
}
