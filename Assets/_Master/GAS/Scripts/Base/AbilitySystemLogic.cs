using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Business logic for Ability System operations.
    /// Stateless service that operates on AbilitySystemData.
    /// Can be registered as Singleton in VContainer.
    /// </summary>
    public readonly struct GameplayEffectAppliedEvent
    {
        public readonly int SourceInstanceID;
        public readonly int TargetInstanceID;
        public readonly GameplayEffect Effect;
        public readonly GameplayAbilityData SourceAbility;
        public readonly AbilitySystemComponent TargetASC;

        public GameplayEffectAppliedEvent(
            int sourceInstanceID,
            int targetInstanceID,
            GameplayEffect effect,
            GameplayAbilityData sourceAbility,
            AbilitySystemComponent targetASC)
        {
            SourceInstanceID = sourceInstanceID;
            TargetInstanceID = targetInstanceID;
            Effect = effect;
            SourceAbility = sourceAbility;
            TargetASC = targetASC;
        }
    }

    public class AbilitySystemLogic
    {
        private readonly GameplayAbilityLogic _abilityLogic;
        private readonly GameplayEffectService _effectService;
        private readonly FD.IEventBus _eventBus;

        public AbilitySystemLogic(GameplayAbilityLogic abilityLogic, GameplayEffectService effectService, FD.IEventBus eventBus)
        {
            this._abilityLogic = abilityLogic;
            this._effectService = effectService;
            this._eventBus = eventBus;
        }
        #region Cooldowns

        public void UpdateCooldowns(AbilitySystemData data, float deltaTime)
        {
            if (data.AbilityCooldowns.Count == 0) return;

            var cooldownKeys = data.AbilityCooldowns.Keys.ToList();
            foreach (var ability in cooldownKeys)
            {
                var spec = GetAbilitySpec(data, ability);
                float multiplier = GetCooldownMultiplier(data, spec);

                // If multiplier is near 0 (infinite cooldown/paused), we don't decrement
                if (multiplier > 0.0001f)
                {
                    // Scale the time decrement: if multiplier is 0.5 (2x fast), 
                    // we remove 1.0s of "base time" for every 0.5s of "real time".
                    data.AbilityCooldowns[ability] -= deltaTime / multiplier;
                }

                if (data.AbilityCooldowns[ability] <= 0)
                    data.AbilityCooldowns.Remove(ability);
            }
        }

        public void StartCooldown(AbilitySystemData data, GameplayAbilityData ability, float baseDuration)
        {
            // Stores the duration as "Base Time". 
            // The actual real-world time it takes to expire will be (baseDuration * currentMultiplier).
            data.AbilityCooldowns[ability] = baseDuration;
        }

        public bool IsAbilityOnCooldown(AbilitySystemData data, GameplayAbilityData ability)
        {
            if (ability == null)
                return false;

            return GetAbilityCooldownRemaining(data, ability) > 0.001f;
        }

        public float GetAbilityCooldownRemaining(AbilitySystemData data, GameplayAbilityData ability)
        {
            if (ability == null)
                return 0f;

            if (data.AbilityCooldowns.TryGetValue(ability, out float remainingBaseTime))
            {
                var spec = GetAbilitySpec(data, ability);
                float multiplier = GetCooldownMultiplier(data, spec);
                return remainingBaseTime * multiplier;
            }
            return 0f;
        }

        private float GetCooldownMultiplier(AbilitySystemData data, GameplayAbilitySpec spec)
        {
            if (spec == null || data.AttributeSet == null) return 1f;

            var attr = data.AttributeSet.GetAttribute(spec.cooldownRateAttr);
            return attr != null ? Mathf.Max(0f, attr.CurrentValue) : 1f;
        }

        #endregion

        #region Tags

        public void AddTags(AbilitySystemData data, params GameplayTag[] tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                if (tag != GameplayTag.None)
                {
                    byte tagByte = (byte)tag;
                    int newCount = 0;
                    if (data.ActiveTagCounts.ContainsKey(tagByte))
                    {
                        newCount = ++data.ActiveTagCounts[tagByte];
                    }
                    else
                    {
                        newCount = 1;
                        data.ActiveTagCounts[tagByte] = 1;
                    }

                    if (data.Owner != null)
                    {
                        _eventBus.Publish(new GameplayTagChangedEvent(data.Owner.gameObject.GetInstanceID(), tag, newCount));
                    }
                }
            }
        }

        public void RemoveTags(AbilitySystemData data, params GameplayTag[] tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                byte tagByte = (byte)tag;
                if (data.ActiveTagCounts.ContainsKey(tagByte))
                {
                    int newCount = --data.ActiveTagCounts[tagByte];

                    if (newCount <= 0)
                    {
                        data.ActiveTagCounts.Remove(tagByte);
                    }

                    if (data.Owner != null)
                    {
                        _eventBus.Publish(new GameplayTagChangedEvent(data.Owner.gameObject.GetInstanceID(), tag, newCount));
                    }
                }
            }
        }

        public bool HasAnyTags(AbilitySystemData data, params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;

            foreach (var tag in tags)
            {
                if (data.ActiveTagCounts.ContainsKey((byte)tag))
                    return true;
            }
            return false;
        }

        public bool HasAllTags(AbilitySystemData data, params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;

            foreach (var tag in tags)
            {
                if (!data.ActiveTagCounts.ContainsKey((byte)tag))
                    return false;
            }
            return true;
        }

        public int GetTagCount(AbilitySystemData data, GameplayTag tag)
        {
            byte tagByte = (byte)tag;
            return data.ActiveTagCounts.ContainsKey(tagByte) ? data.ActiveTagCounts[tagByte] : 0;
        }

        public List<GameplayTag> GetActiveTags(AbilitySystemData data)
        {
            var tags = new List<GameplayTag>();
            foreach (var tagByte in data.ActiveTagCounts.Keys)
            {
                tags.Add((GameplayTag)tagByte);
            }
            return tags;
        }

        #endregion

        #region Abilities

        public GameplayAbilitySpec GiveAbility(AbilitySystemData data, GameplayAbilityData ability, float level = 1f)
        {
            if (ability == null)
            {
                //Debug.LogWarning("Cannot grant a null ability.");
                return null;
            }

            if (data.SpecLookup.TryGetValue(ability, out var existingSpec))
            {
                existingSpec.SetLevel(level);
                return existingSpec;
            }

            var spec = new GameplayAbilitySpec(ability, level);
            data.AbilitySpecs.Add(spec);
            data.SpecLookup[ability] = spec;

            if (!data.GrantedAbilities.Contains(ability))
            {
                data.GrantedAbilities.Add(ability);
            }

            return spec;
        }

        public GameplayAbilitySpec GetAbilitySpec(AbilitySystemData data, GameplayAbilityData ability)
        {
            if (ability == null)
                return null;

            data.SpecLookup.TryGetValue(ability, out var spec);
            return spec;
        }

        public void SetAbilityLevel(AbilitySystemData data, GameplayAbilityData ability, float level)
        {
            var spec = GetAbilitySpec(data, ability);
            spec?.SetLevel(level);
        }

        public bool TryActivateAbility(AbilitySystemData data, AbilitySystemComponent asc, GameplayAbilityData ability)
        {
            var spec = GetAbilitySpec(data, ability);
            if (spec == null)
            {
#if UNITY_EDITOR
                //Debug.LogWarning($"Ability {ability?.abilityName} is not granted to {data.Owner.name}");
#endif
                return false;
            }

            return TryActivateAbility(data, asc, spec);
        }

        public bool TryActivateAbility(AbilitySystemData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (spec == null || spec.Definition == null)
            {
#if UNITY_EDITOR
                //Debug.LogWarning($"Invalid ability spec on {data.Owner.name}");
#endif
                return false;
            }

            var ability = spec.Definition;

            // Use GameplayAbilityLogic to check and activate
            if (_abilityLogic.CanActivateAbility(ability, asc, spec))
            {
                _abilityLogic.ActivateAbility(ability, asc, spec);
                if (!data.ActiveAbilities.Contains(ability))
                {
                    data.ActiveAbilities.Add(ability);
                }
                return true;
            }

            return false;
        }

        public bool TryActivateAbilityByIndex(AbilitySystemData data, AbilitySystemComponent asc, int index)
        {
            if (index < 0 || index >= data.AbilitySpecs.Count)
                return false;

            return TryActivateAbility(data, asc, data.AbilitySpecs[index]);
        }

        public void CancelAbility(AbilitySystemData data, AbilitySystemComponent asc, GameplayAbilityData ability)
        {
            if (ability == null)
                return;

            var spec = GetAbilitySpec(data, ability);
            if (spec == null)
                return;

            if (spec.IsActive)
            {
                _abilityLogic.CancelAbility(ability, asc, spec);
            }
        }

        public void CancelAbilitiesWithTags(AbilitySystemData data, AbilitySystemComponent asc, GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return;

            var abilitiesToCancel = data.ActiveAbilities
                .Where(a => a.abilityTags != null && a.abilityTags.Any(abilityTag => tags.Contains(abilityTag)))
                .ToList();

            foreach (var ability in abilitiesToCancel)
            {
                CancelAbility(data, asc, ability);
            }
        }

        public void NotifyAbilityEnded(AbilitySystemData data, GameplayAbilityData ability)
        {
            if (ability == null)
                return;

            if (data.ActiveAbilities.Contains(ability))
            {
                data.ActiveAbilities.Remove(ability);
            }
        }

        #endregion

        #region Gameplay Effects

        public void UpdateGameplayEffects(AbilitySystemData data, float deltaTime)
        {
            for (int i = data.ActiveGameplayEffects.Count - 1; i >= 0; i--)
            {
                if (i < data.ActiveGameplayEffects.Count)
                {
                    data.ActiveGameplayEffects[i].Update(deltaTime);
                }
            }
        }

        public ActiveGameplayEffect ApplyGameplayEffectToTarget(
            GameplayEffect effect,
            AbilitySystemComponent target,
            AbilitySystemComponent source,
            float effectLevel = 1f,
            GameplayAbilityData sourceAbility = null)
        {
            if (effect == null || target == null)
                return null;

            var targetData = target.GetData();

            // Check if effect can be applied
            if (!_effectService.CanApplyTo(effect, target))
            {
                //Debug.LogWarning($"Cannot apply {effect.effectName} to {targetData.Owner.name}");
                return null;
            }

            // Handle stacking
            if (effect.allowStacking)
            {
                var existingEffect = targetData.ActiveGameplayEffects.FirstOrDefault(e => e.Effect == effect);
                if (existingEffect != null)
                {
                    if (existingEffect.AddStack())
                    {
                        // CRITICAL FIX: Refresh attribute modifiers with new stack count
                        _effectService.RefreshModifiers(existingEffect);
                    }

#if UNITY_EDITOR
                    //Debug.Log($"Stacked {effect.effectName} on {targetData.Owner.name} (x{existingEffect.StackCount})");
#endif
                    // Always return the existing effect to prevent duplicate instances bypassing maxStacks
                    // Even if AddStack returns false (max stacks reached), we shouldn't create a new instance
                    return existingEffect;
                }
            }
            else
            {
                // Non-stacking: if this exact effect is already active, remove the old instance
                // before adding a fresh one (duration refresh). Prevents duplicate accumulation.
                var existingEffect = targetData.ActiveGameplayEffects.FirstOrDefault(e => e.Effect == effect);
                if (existingEffect != null)
                {
                    RemoveGameplayEffect(targetData, existingEffect);
                }
            }

            // Create context for this application
            var context = new GameplayEffectContext
            {
                SourceASC = source,
                TargetASC = target,
                Level = effectLevel
            };

            // Create active effect
            var activeEffect = new ActiveGameplayEffect(effect, source, target, effectLevel, _effectService, context);

            // Handle Instant effects directly without giving them persistent tags
            if (effect.durationType == EGameplayEffectDurationType.Instant)
            {
                if (targetData.AttributeSet != null)
                {
                    foreach (var modifier in effect.modifiers)
                    {
                        _effectService.ApplyModifierWithAggregation(
                            effect, modifier, source, target, effectLevel, 1f, null, true, null);
                    }
                }

                // Clean up tags that should be removed
                if (effect.removeTagsOnApplication != null && effect.removeTagsOnApplication.Length > 0)
                {
                    RemoveTags(targetData, effect.removeTagsOnApplication);
                }

                _eventBus.Publish(new GameplayEffectAppliedEvent(
                    source?.UnitInstanceID ?? -1,
                    target.UnitInstanceID,
                    effect,
                    sourceAbility,
                    target
                ));

                return activeEffect; // Does not add to ActiveGameplayEffects
            }

            // Apply tags (only for Duration/Infinite effects)
            if (effect.grantedTags != null && effect.grantedTags.Length > 0)
            {
                AddTags(targetData, effect.grantedTags);
            }

            // Remove tags
            if (effect.removeTagsOnApplication != null && effect.removeTagsOnApplication.Length > 0)
            {
                RemoveTags(targetData, effect.removeTagsOnApplication);
            }

            // Apply initial modifiers for non-periodic duration/infinite effects
            if (!effect.isPeriodic && targetData.AttributeSet != null)
            {
                foreach (var modifier in effect.modifiers)
                {
                    _effectService.ApplyModifierWithAggregation(
                        effect,
                        modifier,
                        source,
                        target,
                        effectLevel,
                        1f,
                        activeEffect,
                        false,
                        null);
                }
            }

            // Subscribe to expiration
            activeEffect.OnEffectExpired += target.OnGameplayEffectExpired;
            activeEffect.OnEffectRemoved += target.OnGameplayEffectRemoved;

            // Add to active effects
            targetData.ActiveGameplayEffects.Add(activeEffect);

            _eventBus.Publish(new GameplayEffectAppliedEvent(
                source?.UnitInstanceID ?? -1,
                target.UnitInstanceID,
                effect,
                sourceAbility,
                target
            ));

            return activeEffect;
        }

        public void RemoveGameplayEffect(AbilitySystemData data, ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null || !data.ActiveGameplayEffects.Contains(activeEffect))
                return;

            // Remove modifiers from all affected attributes
            var affectedAttributes = activeEffect.GetAffectedAttributes();
            foreach (var attribute in affectedAttributes)
            {
                attribute.RemoveModifiersFromEffect(activeEffect);
            }

            // Remove tags
            if (activeEffect.Effect.grantedTags != null)
            {
                RemoveTags(data, activeEffect.Effect.grantedTags);
            }

            data.ActiveGameplayEffects.Remove(activeEffect);
        }

        public void RemoveGameplayEffectsWithTags(AbilitySystemData data, params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return;

            var effectsToRemove = data.ActiveGameplayEffects
                .Where(e => e.Effect.grantedTags != null && e.Effect.grantedTags.Any(effectTag => tags.Contains(effectTag)))
                .ToList();

            foreach (var effect in effectsToRemove)
            {
                RemoveGameplayEffect(data, effect);
            }
        }

        public void RemoveAllGameplayEffects(AbilitySystemData data)
        {
            var effects = data.ActiveGameplayEffects.ToList();
            foreach (var effect in effects)
            {
                RemoveGameplayEffect(data, effect);
            }
        }

        public bool HasActiveEffect(AbilitySystemData data, GameplayEffect effect)
        {
            return data.ActiveGameplayEffects.Any(e => e.Effect == effect);
        }

        #endregion
    }
}
