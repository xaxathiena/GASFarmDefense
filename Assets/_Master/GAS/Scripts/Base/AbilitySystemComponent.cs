using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer.Unity;

namespace GAS
{
    /// <summary>
    /// Component that manages abilities for a GameObject (similar to UE's AbilitySystemComponent)
    /// Refactored to use Data (state) + Logic (behavior) separation for better DI and testability.
    /// </summary>
    public class AbilitySystemComponent
    {
        // Dependencies
        private readonly IDebugService debug;
        private readonly AbilitySystemLogic logic;
        private readonly GameplayAbilityLogic abilityLogic;

        // Data (all state is here)
        private readonly AbilitySystemData data;

        // Public properties
        public AttributeSet AttributeSet => data.AttributeSet;
        public string Id => data.Id;

        public AbilitySystemComponent(IDebugService debug, AbilitySystemLogic logic, GameplayAbilityLogic abilityLogic)
        {
            this.debug = debug;
            this.logic = logic;
            this.abilityLogic = abilityLogic;
            this.data = new AbilitySystemData
            {
                Id = Guid.NewGuid().ToString()
            };
        }

        public void Tick()
        {
            // Delegate all logic to the logic service
            logic.UpdateCooldowns(data, Time.deltaTime);
            logic.UpdateGameplayEffects(data, Time.deltaTime);
        }

        public void InitOwner(Transform owner)
        {
            data.Owner = owner;
        }

        public virtual Transform GetOwner()
        {
            return data.Owner;
        }

        // Internal accessor for logic layer
        internal AbilitySystemData GetData() => data;

#if UNITY_EDITOR
        // Editor-only accessors for debug tools
        public List<GameplayAbilityData> EditorGetGrantedAbilities() => data.GrantedAbilities;
        public Dictionary<GameplayAbilityData, float> EditorGetAbilityCooldowns() => data.AbilityCooldowns;
        public List<ActiveGameplayEffect> EditorGetActiveEffects() => data.ActiveGameplayEffects;
#endif

        #region Abilities

        /// <summary>
        /// Grant an ability to this component with a specific starting level.
        /// </summary>
        public GameplayAbilitySpec GiveAbility(GameplayAbilityData ability, float level = 1f)
        {
            return logic.GiveAbility(data, ability, level);
        }

        /// <summary>
        /// Try to fetch an existing runtime spec for the provided ability definition.
        /// </summary>
        public GameplayAbilitySpec GetAbilitySpec(GameplayAbilityData ability)
        {
            return logic.GetAbilitySpec(data, ability);
        }

        /// <summary>
        /// Force-set the level for an already granted ability.
        /// </summary>
        public void SetAbilityLevel(GameplayAbilityData ability, float level)
        {
            logic.SetAbilityLevel(data, ability, level);
        }

        /// <summary>
        /// Try to activate an ability
        /// </summary>
        public bool TryActivateAbility(GameplayAbilityData ability)
        {
            return logic.TryActivateAbility(data, this, ability);
        }

        /// <summary>
        /// Try to activate a specific ability spec instance.
        /// </summary>
        public bool TryActivateAbility(GameplayAbilitySpec spec)
        {
            return logic.TryActivateAbility(data, this, spec);
        }

        /// <summary>
        /// Try to activate an ability by index
        /// </summary>
        public bool TryActivateAbilityByIndex(int index)
        {
            return logic.TryActivateAbilityByIndex(data, this, index);
        }

        /// <summary>
        /// Cancel an ability
        /// </summary>
        public void CancelAbility(GameplayAbilityData ability)
        {
            logic.CancelAbility(data, this, ability);
        }

        /// <summary>
        /// End an ability (complete it normally)
        /// </summary>
        public void EndAbility(GameplayAbilityData ability)
        {
            var spec = GetAbilitySpec(ability);
            if (spec != null && abilityLogic != null)
            {
                abilityLogic.EndAbility(ability, this, spec);
            }
        }

        /// <summary>
        /// Cancel abilities that have any of the specified tags
        /// </summary>
        public void CancelAbilitiesWithTags(GameplayTag[] tags)
        {
            logic.CancelAbilitiesWithTags(data, this, tags);
        }

        internal void NotifyAbilityEnded(GameplayAbilityData ability)
        {
            logic.NotifyAbilityEnded(data, ability);
        }

        #endregion

        #region Tags

        /// <summary>
        /// Add gameplay tags (with reference counting)
        /// Multiple effects can grant the same tag - tag will only be removed when all are gone
        /// </summary>
        public void AddTags(params GameplayTag[] tags)
        {
            logic.AddTags(data, tags);
        }

        /// <summary>
        /// Remove gameplay tags (with reference counting)
        /// Tag is only removed when count reaches 0 (no more effects granting it)
        /// </summary>
        public void RemoveTags(params GameplayTag[] tags)
        {
            logic.RemoveTags(data, tags);
        }

        /// <summary>
        /// Check if has any of the specified tags (OR logic)
        /// </summary>
        public bool HasAnyTags(params GameplayTag[] tags)
        {
            return logic.HasAnyTags(data, tags);
        }

        /// <summary>
        /// Check if has all of the specified tags (AND logic)
        /// </summary>
        public bool HasAllTags(params GameplayTag[] tags)
        {
            return logic.HasAllTags(data, tags);
        }

        /// <summary>
        /// Get tag count for debugging (how many effects are granting this tag)
        /// </summary>
        public int GetTagCount(GameplayTag tag)
        {
            return logic.GetTagCount(data, tag);
        }

        /// <summary>
        /// Get all active tags (for debugging/display)
        /// </summary>
        public List<GameplayTag> GetActiveTags()
        {
            return logic.GetActiveTags(data);
        }

        #endregion

        #region Attribute Set

        /// <summary>
        /// Initialize attribute set
        /// </summary>
        public void InitializeAttributeSet(AttributeSet attributeSet)
        {
            data.AttributeSet = attributeSet;
            if (attributeSet == null)
            {
                //Debug.LogError("AttributeSet is null!");
                return;
            }
            attributeSet.InitAttributeSet(this);
        }

        /// <summary>
        /// Get attribute set
        /// </summary>
        public T GetAttributeSet<T>() where T : AttributeSet
        {
            return data.AttributeSet as T;
        }

        #endregion

        #region Cooldowns

        /// <summary>
        /// Start cooldown for an ability
        /// </summary>
        public void StartCooldown(GameplayAbilityData ability, float duration)
        {
            logic.StartCooldown(data, ability, duration);
        }

        /// <summary>
        /// Check if ability is on cooldown
        /// </summary>
        public bool IsAbilityOnCooldown(GameplayAbilityData ability)
        {
            return logic.IsAbilityOnCooldown(data, ability);
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetAbilityCooldownRemaining(GameplayAbilityData ability)
        {
            return logic.GetAbilityCooldownRemaining(data, ability);
        }

        #endregion

        #region Gameplay Effects

        /// <summary>
        /// Apply a gameplay effect to this component
        /// </summary>
        public ActiveGameplayEffect ApplyGameplayEffectToSelf(GameplayEffect effect, AbilitySystemComponent source = null, float effectLevel = 1f)
        {
            return ApplyGameplayEffectToTarget(effect, this, source ?? this, effectLevel);
        }

        /// <summary>
        /// Apply a gameplay effect to a target
        /// </summary>
        public ActiveGameplayEffect ApplyGameplayEffectToTarget(GameplayEffect effect, AbilitySystemComponent target, AbilitySystemComponent source, float effectLevel = 1f)
        {
            return logic.ApplyGameplayEffectToTarget(effect, target, source, effectLevel);
        }

        /// <summary>
        /// Remove a specific active gameplay effect
        /// </summary>
        public void RemoveGameplayEffect(ActiveGameplayEffect activeEffect)
        {
            logic.RemoveGameplayEffect(data, activeEffect);
        }

        /// <summary>
        /// Remove all gameplay effects with specific tags
        /// </summary>
        public void RemoveGameplayEffectsWithTags(params GameplayTag[] tags)
        {
            logic.RemoveGameplayEffectsWithTags(data, tags);
        }

        /// <summary>
        /// Remove all gameplay effects
        /// </summary>
        public void RemoveAllGameplayEffects()
        {
            logic.RemoveAllGameplayEffects(data);
        }

        /// <summary>
        /// Get all active gameplay effects
        /// </summary>
        public List<ActiveGameplayEffect> GetActiveGameplayEffects()
        {
            return new List<ActiveGameplayEffect>(data.ActiveGameplayEffects);
        }

        /// <summary>
        /// Check if has active effect
        /// </summary>
        public bool HasActiveEffect(GameplayEffect effect)
        {
            return logic.HasActiveEffect(data, effect);
        }

        /// <summary>
        /// Called when a gameplay effect expires
        /// </summary>
        internal void OnGameplayEffectExpired(ActiveGameplayEffect effect)
        {
            RemoveGameplayEffect(effect);
        }

        /// <summary>
        /// Called when a gameplay effect is removed
        /// </summary>
        internal void OnGameplayEffectRemoved(ActiveGameplayEffect effect)
        {
            RemoveGameplayEffect(effect);
        }

        #endregion
    }
}
