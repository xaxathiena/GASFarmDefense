using UnityEngine;

namespace GAS
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
            Health = new GameplayAttribute();
            Health.SetCurrentValue(maxHealth);

            Energy = new GameplayAttribute();
            Energy.SetCurrentValue(maxEnergy);

            Shield = new GameplayAttribute();
            Shield.SetCurrentValue(maxShield);
            Speed = new GameplayAttribute();
            Speed.SetCurrentValue(5f);

            Damage = new GameplayAttribute();
            Damage.SetCurrentValue(10f);
            Damage.SetCurrentValue(10f);
            CritChance = new GameplayAttribute();
            CritChance.SetCurrentValue(0.1f);
            CritChance.SetCurrentValue(0.1f);
            CritDamage = new GameplayAttribute();
            CritDamage.SetCurrentValue(0.5f);
            CritDamage.SetCurrentValue(0.5f);
            
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
