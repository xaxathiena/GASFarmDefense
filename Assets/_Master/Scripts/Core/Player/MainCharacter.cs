using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
using Abel.GAS;
using FD.Ability;
using UnityEngine;
namespace FD.Player
{
    public class MainCharacter : MonoBehaviour, IAbilitySystemComponent
    {
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private GameplayEffect initialEffect;
        [SerializeField] private FDAttributeSet attributeSet;

        public AbilitySystemComponent AbilitySystemComponent => abilitySystemComponent;

        private void Awake()
        {
            if(abilitySystemComponent == null)
            {
                abilitySystemComponent = GetComponent<AbilitySystemComponent>();
            }
            attributeSet = new FDAttributeSet();
            abilitySystemComponent.InitializeAttributeSet(attributeSet);
        }
        void Start()
        {
            InitInitialEffects();
        }
        private void InitInitialEffects()
        {
            if (initialEffect != null)
            {
                abilitySystemComponent.ApplyGameplayEffectToSelf(initialEffect);
            }
        }
    }
}

