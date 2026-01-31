using GAS;
using FD.Ability;
using UnityEngine;

namespace FD.TrainingArea
{
    /// <summary>
    /// Dummy enemy for battle training with regeneration and damage indicators
    /// </summary>
    public class DummyEnemy : MonoBehaviour, IAbilitySystemComponent
    {
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private GameplayEffect initialEffect;
        [SerializeField] private FDAttributeSet attributeSet;
        
        [Header("Training Settings")]
        [SerializeField] private bool autoRegen = true;
        [SerializeField] private float regenRate = 10f; // HP per second
        [SerializeField] private bool invulnerable = false;
        [SerializeField] private bool showDamageNumbers = true;

        public AbilitySystemComponent AbilitySystemComponent => abilitySystemComponent;
        public FDAttributeSet AttributeSet => attributeSet;

        private void Awake()
        {
            if (abilitySystemComponent == null)
            {
                abilitySystemComponent = GetComponent<AbilitySystemComponent>();
            }
            
            attributeSet = new FDAttributeSet();
            abilitySystemComponent.InitializeAttributeSet(attributeSet);
        }

        private void Start()
        {
            InitInitialEffects();
        }

        private void Update()
        {
            if (autoRegen && attributeSet != null)
            {
                float currentHealth = attributeSet.Health.CurrentValue;
                float maxHealth = attributeSet.MaxHealth.CurrentValue;
                
                if (currentHealth < maxHealth)
                {
                    float newHealth = Mathf.Min(currentHealth + regenRate * Time.deltaTime, maxHealth);
                    attributeSet.Health.SetCurrentValue(newHealth);
                }
            }
        }

        private void InitInitialEffects()
        {
            if (initialEffect != null)
            {
                abilitySystemComponent.ApplyGameplayEffectToSelf(initialEffect);
            }
        }

        public void SetInvulnerable(bool value)
        {
            invulnerable = value;
        }

        public void SetAutoRegen(bool value)
        {
            autoRegen = value;
        }

        public void SetRegenRate(float rate)
        {
            regenRate = rate;
        }

        public void ResetHealth()
        {
            if (attributeSet != null)
            {
                attributeSet.Health.SetCurrentValue(attributeSet.MaxHealth.CurrentValue);
            }
        }
    }
}
