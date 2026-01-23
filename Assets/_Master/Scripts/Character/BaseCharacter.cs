using _Master.Base.Ability;
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
            
            InitializeAttributeSet();
        }

        protected virtual void Start()
        {
            InitInitialEffects();
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
    }
}
