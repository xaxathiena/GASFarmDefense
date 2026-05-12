using Abel.GAS.Attributes;
using UnityEngine;

namespace Abel.GAS.Samples.OrbCombat
{
    public class OrbAttributeSet : AttributeSet
    {
        public GameplayAttribute Health { get; private set; }
        public GameplayAttribute MaxHealth { get; private set; }
        public GameplayAttribute Mana { get; private set; }
        public GameplayAttribute MaxMana { get; private set; }
        public GameplayAttribute Speed { get; private set; }

        public OrbAttributeSet()
        {
            Health = new GameplayAttribute(100f);
            MaxHealth = new GameplayAttribute(100f);
            Mana = new GameplayAttribute(50f);
            MaxMana = new GameplayAttribute(100f);
            Speed = new GameplayAttribute(5f);
        }

        protected override void OnAttributeSetInitialized()
        {
            RegisterAttribute(EGameplayAttributeType.Health, Health);
            RegisterAttribute(EGameplayAttributeType.MaxHealth, MaxHealth);
            RegisterAttribute(EGameplayAttributeType.Mana, Mana);
            RegisterAttribute(EGameplayAttributeType.MaxMana, MaxMana);
            RegisterAttribute(EGameplayAttributeType.MoveSpeed, Speed);
        }

        protected override void PreAttributeChange(GameplayAttribute attribute, float newValue)
        {
            if (attribute == Health)
            {
                // Clamp health between 0 and MaxHealth
                newValue = Mathf.Clamp(newValue, 0f, MaxHealth.CurrentValue);
            }
        }
    }
}
