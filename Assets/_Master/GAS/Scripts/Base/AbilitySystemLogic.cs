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
    public class AbilitySystemLogic
    {
        private readonly GameplayAbilityLogic _abilityLogic;
        private readonly GameplayEffectService _effectService;

        public AbilitySystemLogic(GameplayAbilityLogic abilityLogic, GameplayEffectService effectService)
        {
            this._abilityLogic = abilityLogic;
            this._effectService = effectService;
        }
        #region Cooldowns

        public void UpdateCooldowns(AbilitySystemData data, float deltaTime)
        {
            var cooldownKeys = data.AbilityCooldowns.Keys.ToList();
            foreach (var ability in cooldownKeys)
            {
                data.AbilityCooldowns[ability] -= deltaTime;
                if (data.AbilityCooldowns[ability] <= 0)
                    data.AbilityCooldowns.Remove(ability);
            }
        }

        public void StartCooldown(AbilitySystemData data, GameplayAbilityData ability, float duration)
        {
            data.AbilityCooldowns[ability] = duration;
        }

        public bool IsAbilityOnCooldown(AbilitySystemData data, GameplayAbilityData ability)
        {
            if (ability == null)
                return false;
            return data.AbilityCooldowns.ContainsKey(ability) && data.AbilityCooldowns[ability] > 0;
        }

        public float GetAbilityCooldownRemaining(AbilitySystemData data, GameplayAbilityData ability)
        {
            if (ability == null)
                return 0f;

            if (data.AbilityCooldowns.TryGetValue(ability, out float remaining))
                return remaining;
            return 0f;
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
                    if (data.ActiveTagCounts.ContainsKey(tagByte))
                    {
                        data.ActiveTagCounts[tagByte]++;
                    }
                    else
                    {
                        data.ActiveTagCounts[tagByte] = 1;
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
                    data.ActiveTagCounts[tagByte]--;

                    if (data.ActiveTagCounts[tagByte] <= 0)
                    {
                        data.ActiveTagCounts.Remove(tagByte);
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
            float effectLevel = 1f)
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
#if UNITY_EDITOR
                        //Debug.Log($"Stacked {effect.effectName} on {targetData.Owner.name} (x{existingEffect.StackCount})");
#endif
                        return existingEffect;
                    }
                }
            }

            // Create active effect
            var activeEffect = new ActiveGameplayEffect(effect, source, target, effectLevel, _effectService);

            // Apply tags
            if (effect.grantedTags != null && effect.grantedTags.Length > 0)
            {
                AddTags(targetData, effect.grantedTags);
            }

            // Remove tags
            if (effect.removeTagsOnApplication != null && effect.removeTagsOnApplication.Length > 0)
            {
                RemoveTags(targetData, effect.removeTagsOnApplication);
            }

            // Apply modifiers for instant effects
            if (effect.durationType == EGameplayEffectDurationType.Instant)
            {
                if (targetData.AttributeSet != null)
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
                            null,
                            true,
                            null);
                    }
                }

                return activeEffect;
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
