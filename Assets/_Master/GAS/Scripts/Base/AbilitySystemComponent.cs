using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer.Unity;

namespace GAS
{
    /// <summary>
    /// Component that manages abilities for a GameObject (similar to UE's AbilitySystemComponent)
    /// </summary>
    public class AbilitySystemComponent
    {
        private readonly IDebugService debug;
        private AttributeSet attributeSet;

        private List<GameplayAbility> grantedAbilities = new List<GameplayAbility>();

        private readonly List<GameplayAbilitySpec> abilitySpecs = new List<GameplayAbilitySpec>();
        private readonly Dictionary<GameplayAbility, GameplayAbilitySpec> specLookup = new Dictionary<GameplayAbility, GameplayAbilitySpec>();

        // Runtime data
        // Tag reference counting: tracks how many effects are granting each tag
        // Only remove tag when count reaches 0
        private Dictionary<byte, int> activeTagCounts = new Dictionary<byte, int>();
        private Dictionary<GameplayAbility, float> abilityCooldowns = new Dictionary<GameplayAbility, float>();
        private List<GameplayAbility> activeAbilities = new List<GameplayAbility>();
        private List<ActiveGameplayEffect> activeGameplayEffects = new List<ActiveGameplayEffect>();
        private Transform owner;
        public AttributeSet AttributeSet => attributeSet;
        public readonly string Id;

        public AbilitySystemComponent(IDebugService debug)
        {
            this.debug = debug;
            Id = Guid.NewGuid().ToString();
        }
        bool isShow = false;
        public void Tick()
        {
            // Update cooldowns
            var cooldownKeys = abilityCooldowns.Keys.ToList();
            foreach (var ability in cooldownKeys)
            {
                abilityCooldowns[ability] -= Time.deltaTime;
                if (abilityCooldowns[ability] <= 0)
                    abilityCooldowns.Remove(ability);
            }

            // Update active gameplay effects
            UpdateGameplayEffects(Time.deltaTime);
            debug.Log("AbilitySystemComponent Tick called " + Id, Color.yellow);
            //            debug.Log("AbilitySystemComponent Tick called " + Id, Color.yellow);
        }
        public void InitOwner(Transform owner)
        {
            this.owner = owner;
        }
        public virtual Transform GetOwner()
        {
            return owner;
        }
        /// <summary>
        /// Grant an ability to this component with a specific starting level.
        /// </summary>
        public GameplayAbilitySpec GiveAbility(GameplayAbility ability, float level = 1f)
        {
            if (ability == null)
            {
                Debug.LogWarning("Cannot grant a null ability.");
                return null;
            }

            if (specLookup.TryGetValue(ability, out var existingSpec))
            {
                existingSpec.SetLevel(level);
                return existingSpec;
            }

            var spec = new GameplayAbilitySpec(ability, level);
            abilitySpecs.Add(spec);
            specLookup[ability] = spec;

            if (!grantedAbilities.Contains(ability))
            {
                grantedAbilities.Add(ability);
            }

            return spec;
        }

        /// <summary>
        /// Try to fetch an existing runtime spec for the provided ability definition.
        /// </summary>
        public GameplayAbilitySpec GetAbilitySpec(GameplayAbility ability)
        {
            if (ability == null)
                return null;

            specLookup.TryGetValue(ability, out var spec);
            return spec;
        }

        /// <summary>
        /// Force-set the level for an already granted ability.
        /// </summary>
        public void SetAbilityLevel(GameplayAbility ability, float level)
        {
            var spec = GetAbilitySpec(ability);
            spec?.SetLevel(level);
        }

        /// <summary>
        /// Try to activate an ability
        /// </summary>
        public bool TryActivateAbility(GameplayAbility ability)
        {
            var spec = GetAbilitySpec(ability);
            if (spec == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Ability {ability?.abilityName} is not granted to {owner.name}");
#endif
                return false;
            }

            return TryActivateAbility(spec);
        }

        /// <summary>
        /// Try to activate a specific ability spec instance.
        /// </summary>
        public bool TryActivateAbility(GameplayAbilitySpec spec)
        {
            if (spec == null || spec.Definition == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Invalid ability spec on {owner.name}");
#endif
                return false;
            }

            var ability = spec.Definition;

            if (ability.CanActivateAbility(this, spec))
            {
                ability.ActivateAbility(this, spec);
                if (!activeAbilities.Contains(ability))
                {
                    activeAbilities.Add(ability);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to activate an ability by index
        /// </summary>
        public bool TryActivateAbilityByIndex(int index)
        {
            if (index < 0 || index >= abilitySpecs.Count)
                return false;

            return TryActivateAbility(abilitySpecs[index]);
        }

        /// <summary>
        /// Cancel an ability
        /// </summary>
        public void CancelAbility(GameplayAbility ability)
        {
            if (ability == null)
                return;

            var spec = GetAbilitySpec(ability);
            if (spec == null)
                return;

            if (spec.IsActive)
            {
                ability.CancelAbility(this, spec);
            }
        }

        /// <summary>
        /// Cancel abilities that have any of the specified tags
        /// </summary>
        public void CancelAbilitiesWithTags(GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return;

            var abilitiesToCancel = activeAbilities
                .Where(a => a.abilityTags != null && a.abilityTags.Any(abilityTag => tags.Contains(abilityTag)))
                .ToList();

            foreach (var ability in abilitiesToCancel)
            {
                CancelAbility(ability);
            }
        }

        internal void NotifyAbilityEnded(GameplayAbility ability)
        {
            if (ability == null)
                return;

            if (activeAbilities.Contains(ability))
            {
                activeAbilities.Remove(ability);
            }
        }

        #region Tags

        /// <summary>
        /// Add gameplay tags (with reference counting)
        /// Multiple effects can grant the same tag - tag will only be removed when all are gone
        /// </summary>
        public void AddTags(params GameplayTag[] tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                if (tag != GameplayTag.None)
                {
                    byte tagByte = (byte)tag;
                    if (activeTagCounts.ContainsKey(tagByte))
                    {
                        activeTagCounts[tagByte]++;
                    }
                    else
                    {
                        activeTagCounts[tagByte] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// Remove gameplay tags (with reference counting)
        /// Tag is only removed when count reaches 0 (no more effects granting it)
        /// </summary>
        public void RemoveTags(params GameplayTag[] tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                byte tagByte = (byte)tag;
                if (activeTagCounts.ContainsKey(tagByte))
                {
                    activeTagCounts[tagByte]--;

                    // Remove tag completely when count reaches 0
                    if (activeTagCounts[tagByte] <= 0)
                    {
                        activeTagCounts.Remove(tagByte);
                    }
                }
            }
        }

        /// <summary>
        /// Check if has any of the specified tags (OR logic)
        /// </summary>
        public bool HasAnyTags(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;

            foreach (var tag in tags)
            {
                if (activeTagCounts.ContainsKey((byte)tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if has all of the specified tags (AND logic)
        /// </summary>
        public bool HasAllTags(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;

            foreach (var tag in tags)
            {
                if (!activeTagCounts.ContainsKey((byte)tag))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Get tag count for debugging (how many effects are granting this tag)
        /// </summary>
        public int GetTagCount(GameplayTag tag)
        {
            byte tagByte = (byte)tag;
            return activeTagCounts.ContainsKey(tagByte) ? activeTagCounts[tagByte] : 0;
        }

        /// <summary>
        /// Get all active tags (for debugging/display)
        /// </summary>
        public List<GameplayTag> GetActiveTags()
        {
            var tags = new List<GameplayTag>();
            foreach (var tagByte in activeTagCounts.Keys)
            {
                tags.Add((GameplayTag)tagByte);
            }
            return tags;
        }

        #endregion

        #region Attribute Set

        /// <summary>
        /// Initialize attribute set
        /// </summary>
        public void InitializeAttributeSet(AttributeSet attributeSet)
        {
            this.attributeSet = attributeSet;
            if (attributeSet == null)
            {
                Debug.LogError("AttributeSet is null!");
                return;
            }
            attributeSet.InitAttributeSet(this);
        }

        /// <summary>
        /// Get attribute set
        /// </summary>
        public T GetAttributeSet<T>() where T : AttributeSet
        {
            return attributeSet as T;
        }

        #endregion

        #region Cooldowns

        /// <summary>
        /// Start cooldown for an ability
        /// </summary>
        public void StartCooldown(GameplayAbility ability, float duration)
        {
            abilityCooldowns[ability] = duration;
        }

        /// <summary>
        /// Check if ability is on cooldown
        /// </summary>
        public bool IsAbilityOnCooldown(GameplayAbility ability)
        {
            if (ability == null)
                return false;

            return abilityCooldowns.ContainsKey(ability) && abilityCooldowns[ability] > 0;
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetAbilityCooldownRemaining(GameplayAbility ability)
        {
            if (ability == null)
                return 0f;

            if (abilityCooldowns.TryGetValue(ability, out float remaining))
                return remaining;
            return 0f;
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
            if (effect == null || target == null)
                return null;

            // Check if effect can be applied
            if (!effect.CanApplyTo(target))
            {
                Debug.LogWarning($"Cannot apply {effect.effectName} to {target.owner.name}");
                return null;
            }

            // Handle stacking
            if (effect.allowStacking)
            {
                var existingEffect = target.activeGameplayEffects.FirstOrDefault(e => e.Effect == effect);
                if (existingEffect != null)
                {
                    if (existingEffect.AddStack())
                    {
#if UNITY_EDITOR
                        Debug.Log($"Stacked {effect.effectName} on {target.owner.name} (x{existingEffect.StackCount})");
#endif
                        return existingEffect;
                    }
                }
            }

            // Create active effect
            var activeEffect = new ActiveGameplayEffect(effect, source, target, effectLevel);

            // Apply tags
            if (effect.grantedTags != null && effect.grantedTags.Length > 0)
            {
                target.AddTags(effect.grantedTags);
            }

            // Remove tags
            if (effect.removeTagsOnApplication != null && effect.removeTagsOnApplication.Length > 0)
            {
                target.RemoveTags(effect.removeTagsOnApplication);
            }

            // Apply modifiers for instant effects
            if (effect.durationType == EGameplayEffectDurationType.Instant)
            {
                if (target.AttributeSet != null)
                {
                    // Instant effects: Apply directly to BaseValue using aggregation system
                    foreach (var modifier in effect.modifiers)
                    {
                        effect.ApplyModifierWithAggregation(target.AttributeSet, modifier, source, target, effectLevel, activeEffect.StackCount, activeEffect, true);
                    }
                }

                return activeEffect; // Don't add to active list
            }

            // Apply initial modifiers for non-periodic duration/infinite effects using aggregation
            if (!effect.isPeriodic && target.AttributeSet != null)
            {
                foreach (var modifier in effect.modifiers)
                {
                    effect.ApplyModifierWithAggregation(target.AttributeSet, modifier, source, target, effectLevel, activeEffect.StackCount, activeEffect, false);
                }
            }

            // Subscribe to expiration
            activeEffect.OnEffectExpired += target.OnGameplayEffectExpired;
            activeEffect.OnEffectRemoved += target.OnGameplayEffectRemoved;

            // Add to active effects
            target.activeGameplayEffects.Add(activeEffect);

            return activeEffect;
        }

        /// <summary>
        /// Remove a specific active gameplay effect
        /// </summary>
        public void RemoveGameplayEffect(ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null || !activeGameplayEffects.Contains(activeEffect))
                return;

            // Remove modifiers from all affected attributes (triggers recalculation)
            var affectedAttributes = activeEffect.GetAffectedAttributes();
            foreach (var attribute in affectedAttributes)
            {
                attribute.RemoveModifiersFromEffect(activeEffect);
            }

            // Remove tags
            if (activeEffect.Effect.grantedTags != null)
            {
                RemoveTags(activeEffect.Effect.grantedTags);
            }

            activeGameplayEffects.Remove(activeEffect);
        }

        /// <summary>
        /// Remove all gameplay effects with specific tags
        /// </summary>
        public void RemoveGameplayEffectsWithTags(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return;

            var effectsToRemove = activeGameplayEffects
                .Where(e => e.Effect.grantedTags != null && e.Effect.grantedTags.Any(effectTag => tags.Contains(effectTag)))
                .ToList();

            foreach (var effect in effectsToRemove)
            {
                RemoveGameplayEffect(effect);
            }
        }

        /// <summary>
        /// Remove all gameplay effects
        /// </summary>
        public void RemoveAllGameplayEffects()
        {
            var effects = activeGameplayEffects.ToList();
            foreach (var effect in effects)
            {
                RemoveGameplayEffect(effect);
            }
        }

        /// <summary>
        /// Get all active gameplay effects
        /// </summary>
        public List<ActiveGameplayEffect> GetActiveGameplayEffects()
        {
            return new List<ActiveGameplayEffect>(activeGameplayEffects);
        }

        /// <summary>
        /// Check if has active effect
        /// </summary>
        public bool HasActiveEffect(GameplayEffect effect)
        {
            return activeGameplayEffects.Any(e => e.Effect == effect);
        }

        /// <summary>
        /// Update all active gameplay effects
        /// </summary>
        private void UpdateGameplayEffects(float deltaTime)
        {
            for (int i = activeGameplayEffects.Count - 1; i >= 0; i--)
            {
                if (i < activeGameplayEffects.Count)
                {
                    activeGameplayEffects[i].Update(deltaTime);
                }
            }
        }

        /// <summary>
        /// Called when a gameplay effect expires
        /// </summary>
        private void OnGameplayEffectExpired(ActiveGameplayEffect effect)
        {
            RemoveGameplayEffect(effect);
        }

        /// <summary>
        /// Called when a gameplay effect is removed
        /// </summary>
        private void OnGameplayEffectRemoved(ActiveGameplayEffect effect)
        {
            RemoveGameplayEffect(effect);
        }

        #endregion
    }
}
