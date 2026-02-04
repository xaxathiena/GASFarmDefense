using System;
using System.Collections.Generic;
using GAS;
using FD.Ability;
using UnityEngine;

namespace FD.Character
{
    public abstract class BaseCharacter : MonoBehaviour, IAbilitySystemComponent
    {
        [SerializeField] protected AbilitySystemComponent abilitySystemComponent;
        [SerializeField] protected EArmorType armorType = EArmorType.Medium;
        protected FDAttributeSet attributeSet;
        private bool attributeChangeListenersRegistered;

        public AbilitySystemComponent AbilitySystemComponent => abilitySystemComponent;
        public FDAttributeSet AttributeSet => attributeSet;
        public EArmorType ArmorType => armorType;

        public event Action<AttributeChangeInfo> AttributeChanged;

        protected virtual void Awake()
        {
            if(abilitySystemComponent == null)
            {
                abilitySystemComponent = GetComponent<AbilitySystemComponent>();
            }
            abilitySystemComponent.InitOwner(this.gameObject);
            Initialize();
        }

        protected virtual void Update()
        {
            TickManaRegen(Time.deltaTime);
        }
        protected void Initialize()
        {
            attributeSet = new FDAttributeSet();
            attributeSet.armorType = armorType;
            abilitySystemComponent.InitializeAttributeSet(attributeSet);
            RegisterAttributeChangeListeners();
            InitializeAttributeSet();
        }
        protected virtual void InitializeAttributeSet()
        {
            
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

        /// <summary>
        /// Check if character is stunned (has State.Stunned tag)
        /// </summary>
        public bool IsStunned()
        {
            return abilitySystemComponent != null && 
                   abilitySystemComponent.HasAnyTags("State.Stunned");
        }

        /// <summary>
        /// Check if character is immune to CC effects
        /// </summary>
        public bool IsImmune()
        {
            return abilitySystemComponent != null && 
                   abilitySystemComponent.HasAnyTags("State.Immune", "State.Immune.CC");
        }

        /// <summary>
        /// Check if can perform actions (not stunned, not disabled, etc.)
        /// </summary>
        public virtual bool CanPerformActions()
        {
            if (IsStunned())
            {
                return false;
            }

            // Check other blocking states
            if (abilitySystemComponent != null && 
                abilitySystemComponent.HasAnyTags("State.Disabled", "State.Silenced"))
            {
                return false;
            }

            return true;
        }

        private void RegisterAttributeChangeListeners()
        {
            if (attributeSet == null || attributeChangeListenersRegistered)
            {
                return;
            }

            var attributes = attributeSet.GetAllAttributes();
            if (attributes == null || attributes.Count == 0)
            {
                return;
            }

            foreach (var kvp in attributes)
            {
                var attributeName = kvp.Key;
                var gameplayAttribute = kvp.Value;
                if (gameplayAttribute == null || string.IsNullOrEmpty(attributeName))
                {
                    continue;
                }

                var capturedName = attributeName;
                gameplayAttribute.OnValueChanged += (oldValue, newValue) =>
                {
                    RaiseAttributeChanged(capturedName, oldValue, newValue);
                };
            }

            attributeChangeListenersRegistered = true;
        }

        private void RaiseAttributeChanged(string attributeName, float oldValue, float newValue)
        {
            var changeInfo = new AttributeChangeInfo(attributeName, oldValue, newValue);
            AttributeChanged?.Invoke(changeInfo);
            HandleAttributeChanged(changeInfo);
        }

        protected virtual void HandleAttributeChanged(AttributeChangeInfo changeInfo)
        {
        }

        public readonly struct AttributeChangeInfo
        {
            public AttributeChangeInfo(string attributeName, float oldValue, float newValue)
            {
                AttributeName = attributeName;
                OldValue = oldValue;
                NewValue = newValue;
                ChangeAmount = newValue - oldValue;
                if (Enum.TryParse(attributeName, out EGameplayAttributeType parsed))
                {
                    AttributeType = parsed;
                }
                else
                {
                    AttributeType = null;
                }
            }

            public string AttributeName { get; }
            public float OldValue { get; }
            public float NewValue { get; }
            public float ChangeAmount { get; }
            public EGameplayAttributeType? AttributeType { get; }
        }
    }
}
