using UnityEngine;

namespace _Master.Base.Ability
{
    /// <summary>
    /// Example attribute set with common RPG attributes
    /// </summary>
    [CreateAssetMenu(fileName = "New Attribute Set", menuName = "GAS/Attribute Set")]
    public class ExampleAttributeSet : AttributeSet
    {
        [Header("Primary Attributes")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float maxStamina = 100f;
        
        [Header("Combat Attributes")]
        [SerializeField] private float attackPower = 10f;
        [SerializeField] private float defense = 5f;
        [SerializeField] private float moveSpeed = 5f;
        
        // Runtime attributes
        public GameplayAttribute Health { get; private set; }
        public GameplayAttribute Mana { get; private set; }
        public GameplayAttribute Stamina { get; private set; }
        public GameplayAttribute AttackPower { get; private set; }
        public GameplayAttribute Defense { get; private set; }
        public GameplayAttribute MoveSpeed { get; private set; }
        
        private void OnEnable()
        {
            InitializeAttributes();
        }
        
        private void InitializeAttributes()
        {
            // Initialize primary attributes
            Health = new GameplayAttribute(maxHealth, 0f, maxHealth);
            Mana = new GameplayAttribute(maxMana, 0f, maxMana);
            Stamina = new GameplayAttribute(maxStamina, 0f, maxStamina);
            
            // Initialize combat attributes
            AttackPower = new GameplayAttribute(attackPower, 0f, float.MaxValue);
            Defense = new GameplayAttribute(defense, 0f, float.MaxValue);
            MoveSpeed = new GameplayAttribute(moveSpeed, 0f, float.MaxValue);
            
            // Register attributes to dictionary using enum (type-safe)
            RegisterAttribute(EGameplayAttributeType.Health, Health);
            RegisterAttribute(EGameplayAttributeType.Mana, Mana);
            RegisterAttribute(EGameplayAttributeType.Stamina, Stamina);
            RegisterAttribute(EGameplayAttributeType.AttackPower, AttackPower);
            RegisterAttribute(EGameplayAttributeType.Defense, Defense);
            RegisterAttribute(EGameplayAttributeType.MoveSpeed, MoveSpeed);
            
            // Subscribe to value changes
            Health.OnValueChanged += OnHealthChanged;
            Mana.OnValueChanged += OnManaChanged;
            Stamina.OnValueChanged += OnStaminaChanged;
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
            // Apply defense reduction
            float actualDamage = Mathf.Max(0, damage - Defense.CurrentValue);
            Health.ModifyCurrentValue(-actualDamage);
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
            if (Stamina.CurrentValue >= amount)
            {
                Stamina.ModifyCurrentValue(-amount);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Restore stamina
        /// </summary>
        public void RestoreStamina(float amount)
        {
            Stamina.ModifyCurrentValue(amount);
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
            Health.SetCurrentValue(Health.MaxValue);
            Mana.SetCurrentValue(Mana.MaxValue);
            Stamina.SetCurrentValue(Stamina.MaxValue);
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
