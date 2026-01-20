using UnityEngine;

namespace _Master.Base.Ability
{
    /// <summary>
    /// Example: Custom game with different attributes
    /// Shows how to create your own attribute enum for your specific game
    /// </summary>
    
    // Define your own attribute enum for your game
    public enum EMyCustomAttributes
    {
        Health,
        Energy,      // Instead of Mana
        Shield,      // New attribute
        Speed,
        Damage,
        CritChance,
        CritDamage
    }
    
    /// <summary>
    /// Custom attribute set using custom enum
    /// </summary>
    [CreateAssetMenu(fileName = "Custom Attribute Set", menuName = "GAS/Examples/Custom Attribute Set")]
    public class CustomAttributeSet : AttributeSet
    {
        [Header("Primary Attributes")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float maxShield = 50f;
        
        // Properties
        public GameplayAttribute Health { get; private set; }
        public GameplayAttribute Energy { get; private set; }
        public GameplayAttribute Shield { get; private set; }
        public GameplayAttribute Speed { get; private set; }
        public GameplayAttribute Damage { get; private set; }
        public GameplayAttribute CritChance { get; private set; }
        public GameplayAttribute CritDamage { get; private set; }
        
        private void OnEnable()
        {
            InitializeAttributes();
        }
        
        private void InitializeAttributes()
        {
            Health = new GameplayAttribute(maxHealth, 0f, maxHealth);
            Energy = new GameplayAttribute(maxEnergy, 0f, maxEnergy);
            Shield = new GameplayAttribute(maxShield, 0f, maxShield);
            Speed = new GameplayAttribute(5f, 0f, 20f);
            Damage = new GameplayAttribute(10f, 0f, float.MaxValue);
            CritChance = new GameplayAttribute(5f, 0f, 100f); // Percentage
            CritDamage = new GameplayAttribute(150f, 100f, 300f); // Percentage
            
            // Register using custom enum
            RegisterAttribute(EMyCustomAttributes.Health, Health);
            RegisterAttribute(EMyCustomAttributes.Energy, Energy);
            RegisterAttribute(EMyCustomAttributes.Shield, Shield);
            RegisterAttribute(EMyCustomAttributes.Speed, Speed);
            RegisterAttribute(EMyCustomAttributes.Damage, Damage);
            RegisterAttribute(EMyCustomAttributes.CritChance, CritChance);
            RegisterAttribute(EMyCustomAttributes.CritDamage, CritDamage);
        }
        
        /// <summary>
        /// Example: Access attributes using enum
        /// </summary>
        public void ExampleUsage()
        {
            // Get attribute using enum
            var health = GetAttribute(EMyCustomAttributes.Health);
            health.ModifyCurrentValue(-10);
            
            // Check if has attribute
            if (HasAttribute(EMyCustomAttributes.Shield))
            {
                var shield = GetAttribute(EMyCustomAttributes.Shield);
                Debug.Log($"Shield: {shield.CurrentValue}");
            }
        }
    }
}
