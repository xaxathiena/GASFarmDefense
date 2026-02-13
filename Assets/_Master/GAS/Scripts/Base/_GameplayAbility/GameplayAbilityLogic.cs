using UnityEngine;
using System;
using System.Collections.Generic;

namespace GAS
{
    /// <summary>
    /// Business logic for GameplayAbility operations.
    /// Stateless service that operates on GameplayAbility data and AbilitySystemComponent.
    /// Can be registered as Singleton in VContainer.
    /// </summary>
    public class GameplayAbilityLogic
    {
        private readonly AbilityBehaviourRegistry _behaviourRegistry;
        private readonly IDebugService debug;

        public GameplayAbilityLogic(AbilityBehaviourRegistry behaviourRegistry, IDebugService debug)
        {
            _behaviourRegistry = behaviourRegistry;
            this.debug = debug;
        }

        #region Activation Checks

        public bool CanActivateAbility(GameplayAbilityData ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || ability == null)
                return false;

            // InstantEnd abilities cannot re-activate while already active
            if (spec.IsActive && ability.endPolicy == EAbilityEndPolicy.InstantEnd)
                return false;

            if (asc.IsAbilityOnCooldown(ability))
                return false;

            float abilityLevel = GetAbilityLevel(spec);

            float cost = ability.costAmount.GetValueAtLevel(abilityLevel, asc);
            if (cost > 0f)
            {
                var manaAttr = asc.AttributeSet?.GetAttribute(EGameplayAttributeType.Mana);
                if (manaAttr == null)
                {
                    debug.Log($"No Mana attribute found in AttributeSet for ASC {asc.Id}.");
                    return false;
                }
                if (manaAttr.CurrentValue < cost)
                {
                    return false;
                }
            }

            if (asc.HasAnyTags(ability.blockAbilitiesWithTags))
                return false;

            // Check behaviour-specific conditions (e.g., enemies in range)
            var behaviour = _behaviourRegistry.GetBehaviour(ability);
            if (behaviour != null)
            {
                bool behaviourCanActivate = behaviour.CanActivate(ability, asc, spec);
                if (!behaviourCanActivate)
                {
                    // Debug.Log($"[GAL] Behaviour CanActivate returned false for {ability.GetType().Name}");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Activation

        public void ActivateAbility(GameplayAbilityData ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {

            if (!CanActivateAbility(ability, asc, spec))
                return;
            spec.SetActiveState(true);
            // Cancel conflicting abilities
            asc.CancelAbilitiesWithTags(ability.cancelAbilitiesWithTags);
            // Add ability tags
            asc.AddTags(ability.abilityTags);
            float abilityLevel = GetAbilityLevel(spec);
            // Apply cost
            ApplyCost(ability, asc, abilityLevel);
            // Apply cooldown
            ApplyCooldown(ability, asc, abilityLevel);
            // Call behaviour OnActivated
            var behaviour = _behaviourRegistry.GetBehaviour(ability);
            if (behaviour != null)
            {
                string abilityNameDisplay = string.IsNullOrEmpty(ability.abilityName) ? ability.GetType().Name : ability.abilityName;
                behaviour.OnActivated(ability, asc, spec);
            }
            else
            {
                string abilityNameDisplay = string.IsNullOrEmpty(ability.abilityName) ? ability.GetType().Name : ability.abilityName;
            }

            // Auto-end abilities with InstantEnd policy (fire-and-forget)
            // ManualEnd abilities stay active until EndAbility() is called explicitly
            if (ability.endPolicy == EAbilityEndPolicy.InstantEnd)
            {
                EndAbility(ability, asc, spec);
            }
        }

        private void ApplyCost(GameplayAbilityData ability, AbilitySystemComponent asc, float abilityLevel)
        {
            float cost = ability.costAmount.GetValueAtLevel(abilityLevel, asc);
            if (cost > 0f && asc.AttributeSet != null)
            {
                var manaAttr = asc.AttributeSet.GetAttribute(EGameplayAttributeType.Mana);
                if (manaAttr != null)
                {
                    manaAttr.ModifyCurrentValue(-cost);
                }
            }
        }

        private void ApplyCooldown(GameplayAbilityData ability, AbilitySystemComponent asc, float abilityLevel)
        {
            float cooldown = ability.cooldownDuration.GetValueAtLevel(abilityLevel, asc);
            if (cooldown > 0)
            {
                asc.StartCooldown(ability, cooldown);
            }
        }

        #endregion

        #region End/Cancel

        public void EndAbility(GameplayAbilityData ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || !spec.IsActive || ability == null)
                return;

            spec.SetActiveState(false);
            asc.RemoveTags(ability.abilityTags);
            asc.NotifyAbilityEnded(ability);

            // Call behaviour OnEnded
            var behaviour = _behaviourRegistry.GetBehaviour(ability);
            behaviour?.OnEnded(ability, asc, spec);
        }

        public void CancelAbility(GameplayAbilityData ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || !spec.IsActive || ability == null)
                return;

            // Call behaviour OnCancelled
            var behaviour = _behaviourRegistry.GetBehaviour(ability);
            behaviour?.OnCancelled(ability, asc, spec);

            // EndAbility will handle cleanup
            EndAbility(ability, asc, spec);
        }

        #endregion

        #region Effect Application

        public void ApplyEffectToTarget(
            GameplayEffect effect,
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            GameplayAbilityData ability,
            GameplayAbilitySpec spec)
        {
            if (effect == null)
            {
                Debug.LogWarning($"[{ability?.abilityName}] No effect to apply!");
                return;
            }

            // Create context
            var context = CreateEffectContext(source, target, ability, spec);

            // Set as current context for calculation pipeline
            context.MakeCurrent();

            try
            {
                // Apply effect with context
                effect.ApplyModifiers(
                    target.AttributeSet,
                    source,
                    target,
                    context.Level,
                    context.StackCount
                );
            }
            finally
            {
                // Always clear context
                GameplayEffectContext.ClearCurrent();
            }
        }

        private GameplayEffectContext CreateEffectContext(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            GameplayAbilityData ability,
            GameplayAbilitySpec spec)
        {
            return new GameplayEffectContext
            {
                SourceASC = source,
                TargetASC = target,
                SourceAbility = ability,
                Level = GetAbilityLevel(spec)
            };
        }

        #endregion

        #region Helpers

        public float GetAbilityLevel(GameplayAbilitySpec spec)
        {
            return spec?.Level ?? 1f;
        }

        public GameObject GetAbilityOwner(AbilitySystemComponent asc)
        {
            return asc?.GetOwner().gameObject;
        }

        #endregion
    }
}
