using System;
using GAS;
using UnityEngine;
namespace FD.Ability
{
    [Serializable]
    public class FDAttributeSet : AttributeSet
    {
        
        // Runtime attributes
        public GameplayAttribute Health { get; protected set; }
        public GameplayAttribute Mana { get; private set; }
        public GameplayAttribute MaxHealth { get; private set; }
        public GameplayAttribute MaxMana { get; private set; }
        public GameplayAttribute ManaRegen { get; private set; }
        
        public FDAttributeSet()
        {
            // Initialize primary attributes
            Health = new GameplayAttribute();
            Mana = new GameplayAttribute();
            MaxHealth = new GameplayAttribute();
            MaxMana = new GameplayAttribute();
            ManaRegen = new GameplayAttribute();
            
            // Register attributes to dictionary using enum (type-safe)
            RegisterAttribute(EGameplayAttributeType.Health, Health);
            RegisterAttribute(EGameplayAttributeType.Mana, Mana);
            RegisterAttribute(EGameplayAttributeType.MaxHealth, MaxHealth);
            RegisterAttribute(EGameplayAttributeType.MaxMana, MaxMana);
            RegisterAttribute(EGameplayAttributeType.ManaRegen, ManaRegen);
            
            // Subscribe to value changes
            Health.OnValueChanged += OnHealthChanged;
            Mana.OnValueChanged += OnManaChanged;

        }
        
        protected override void OnAttributeSetInitialized()
        {
            base.OnAttributeSetInitialized();
            Debug.Log($"Attribute Set initialized for {ownerASC.gameObject.name}");
        }
        
        #region Attribute Change Callbacks
        
        private void OnHealthChanged(float oldValue, float newValue)
        {
            Debug.Log($"Health changed: {oldValue} -> {newValue}");
            
            // Check for death
            if (newValue <= 0)
            {
                OnDeath();
            }
        }
        
        private void OnManaChanged(float oldValue, float newValue)
        {
            Debug.Log($"Mana changed: {oldValue} -> {newValue}");
        }
        
        private void OnStaminaChanged(float oldValue, float newValue)
        {
            Debug.Log($"Stamina changed: {oldValue} -> {newValue}");
        }
        
        #endregion
        
        #region Convenience Methods
        
        /// <summary>
        /// Deal damage to health
        /// </summary>
        public void TakeDamage(float damage)
        {
        }
        
        /// <summary>
        /// Heal health
        /// </summary>
        public void Heal(float amount)
        {
            Health.ModifyCurrentValue(amount);
        }
        
        /// <summary>
        /// Use mana
        /// </summary>
        public bool UseMana(float amount)
        {
            if (Mana.CurrentValue >= amount)
            {
                Mana.ModifyCurrentValue(-amount);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Restore mana
        /// </summary>
        public void RestoreMana(float amount)
        {
            Mana.ModifyCurrentValue(amount);
        }
        
        /// <summary>
        /// Use stamina
        /// </summary>
        public bool UseStamina(float amount)
        {
            return false;
        }
        
        /// <summary>
        /// Restore stamina
        /// </summary>
        public void RestoreStamina(float amount)
        {
        }
        
        /// <summary>
        /// Check if alive
        /// </summary>
        public bool IsAlive()
        {
            return Health.CurrentValue > 0;
        }
        
        /// <summary>
        /// Full heal
        /// </summary>
        public void FullRestore()
        {
            
        }
        
        #endregion
        
        /// <summary>
        /// Called when health reaches 0
        /// </summary>
        private void OnDeath()
        {
            Debug.Log($"{ownerASC.gameObject.name} has died!");
            
            // Add death tag
            if (ownerASC != null)
            {
                ownerASC.AddTags("State.Dead");
            }
        }
    }
}
