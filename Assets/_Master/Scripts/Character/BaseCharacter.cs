using System.Collections.Generic;
using GAS;
using FD.Ability;
using UnityEngine;

namespace FD.Character
{
    public abstract class BaseCharacter : MonoBehaviour, IAbilitySystemComponent
    {
        [SerializeField] protected AbilitySystemComponent abilitySystemComponent;
        [SerializeField] protected GameplayEffect initialEffect;
        protected FDAttributeSet attributeSet;

        public AbilitySystemComponent AbilitySystemComponent => abilitySystemComponent;
        public FDAttributeSet AttributeSet => attributeSet;

        protected virtual void Awake()
        {
            if(abilitySystemComponent == null)
            {
                abilitySystemComponent = GetComponent<AbilitySystemComponent>();
            }
            abilitySystemComponent.InitOwner(this.gameObject);
            InitializeAttributeSet();
        }

        protected virtual void Start()
        {
            InitInitialEffects();
        }

        protected virtual void Update()
        {
            TickManaRegen(Time.deltaTime);
        }

        protected virtual void InitializeAttributeSet()
        {
            attributeSet = new FDAttributeSet();
            abilitySystemComponent.InitializeAttributeSet(attributeSet);
        }

        protected virtual void InitInitialEffects()
        {
            if (initialEffect != null)
            {
                abilitySystemComponent.ApplyGameplayEffectToSelf(initialEffect);
            }
        }

        protected virtual void TickManaRegen(float deltaTime)
        {
            if (attributeSet == null)
            {
                return;
            }

            float regenPerSecond = attributeSet.ManaRegen.CurrentValue;
            if (regenPerSecond <= 0f)
            {
                return;
            }

            float regenAmount = regenPerSecond * deltaTime;
            float maxMana = attributeSet.MaxMana.CurrentValue;
            float newMana = Mathf.Min(attributeSet.Mana.CurrentValue + regenAmount, maxMana);
            attributeSet.Mana.SetCurrentValue(newMana);
        }

        public virtual List<Transform> GetTargets()
        {
            return new List<Transform>();
        }
    }
}
