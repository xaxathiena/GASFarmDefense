using System;
using GAS;
using UnityEngine;
namespace FD.Ability
{
    [Serializable]
    public class FDAttributeSet : AttributeSet
    {
        [Header("FD Character Configuration")]
        [Tooltip("Loại giáp của character này (Heavy, Light, Medium, etc.)")]
        public EArmorType armorType = EArmorType.Medium;

        // Runtime attributes
        public readonly GameplayAttribute Health = new GameplayAttribute();
        public readonly GameplayAttribute Mana = new GameplayAttribute();
        public readonly GameplayAttribute MaxHealth = new GameplayAttribute();
        public readonly GameplayAttribute MaxMana = new GameplayAttribute();
        public readonly GameplayAttribute ManaRegen = new GameplayAttribute();
        public readonly GameplayAttribute Armor = new GameplayAttribute();
        public readonly GameplayAttribute CriticalChance = new GameplayAttribute();
        public readonly GameplayAttribute CriticalMultiplier = new GameplayAttribute();
        public readonly GameplayAttribute BaseDamage = new GameplayAttribute();
        public readonly GameplayAttribute MoveSpeed = new GameplayAttribute();
        public FDAttributeSet()
        {
            // Initialize primary attributes
            Health = new GameplayAttribute(100);
            Mana = new GameplayAttribute(1000);
            MaxHealth = new GameplayAttribute(100);
            MaxMana = new GameplayAttribute(1000);
            ManaRegen = new GameplayAttribute(2);
            Armor = new GameplayAttribute(10);
            CriticalChance = new GameplayAttribute();
            CriticalMultiplier = new GameplayAttribute();
            BaseDamage = new GameplayAttribute();
            MoveSpeed = new GameplayAttribute();

            // Register attributes to dictionary using enum (type-safe)
            RegisterAttribute(EGameplayAttributeType.Health, Health);
            RegisterAttribute(EGameplayAttributeType.Mana, Mana);
            RegisterAttribute(EGameplayAttributeType.MaxHealth, MaxHealth);
            RegisterAttribute(EGameplayAttributeType.MaxMana, MaxMana);
            RegisterAttribute(EGameplayAttributeType.ManaRegen, ManaRegen);
            RegisterAttribute(EGameplayAttributeType.Armor, Armor);
            RegisterAttribute(EGameplayAttributeType.CriticalChance, CriticalChance);
            RegisterAttribute(EGameplayAttributeType.CriticalMultiplier, CriticalMultiplier);
            RegisterAttribute(EGameplayAttributeType.BaseDamage, BaseDamage);
            RegisterAttribute(EGameplayAttributeType.MoveSpeed, MoveSpeed);

            // Set default values
            CriticalMultiplier.SetBaseValue(2f); // Default 2x crit
            MoveSpeed.SetBaseValue(2f); // Default move speed

            // Subscribe to value changes
            Health.OnValueChanged += OnHealthChanged;
            Mana.OnValueChanged += OnManaChanged;
            Armor.OnValueChanged += OnArmorChanged;
        }

        protected override void OnAttributeSetInitialized()
        {
            base.OnAttributeSetInitialized();
        }

        #region Attribute Change Callbacks

        private void OnHealthChanged(float oldValue, float newValue)
        {
            // Check for death
            if (newValue <= 0 && oldValue > 0)
            {
                OnDeath();
            }
        }

        private void OnManaChanged(float oldValue, float newValue)
        {
            
        }

        private void OnStaminaChanged(float oldValue, float newValue)
        {
           
        }

        private void OnArmorChanged(float oldValue, float newValue)
        {
            float reduction = CalculateArmorReduction(newValue);
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
            Health.SetCurrentValue(MaxHealth.CurrentValue);
            Mana.SetCurrentValue(MaxMana.CurrentValue);
        }

        /// <summary>
        /// Get armor type of this character
        /// </summary>
        public EArmorType GetArmorType() => armorType;

        /// <summary>
        /// Calculate armor reduction percentage based on Warcraft 3 formula
        /// Formula: Reduction = (Armor × 0.06) / (1 + 0.06 × Armor)
        /// </summary>
        private float CalculateArmorReduction(float armor)
        {
            return (armor * 0.06f) / (1f + 0.06f * armor);
        }

        #endregion

        /// <summary>
        /// Called when health reaches 0
        /// </summary>
        private void OnDeath()
        {
            //Debug.Log($"{ownerASC.GetOwner().name} has died!");

            // Add death tag
            if (ownerASC != null)
            {
                ownerASC.AddTags(GameplayTag.State_Dead);
            }
        }
    }
}
