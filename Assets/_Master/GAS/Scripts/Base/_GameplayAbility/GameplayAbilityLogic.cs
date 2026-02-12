using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Business logic for GameplayAbility operations.
    /// Stateless service that operates on GameplayAbility data and AbilitySystemComponent.
    /// Can be registered as Singleton in VContainer.
    /// </summary>
    public class GameplayAbilityLogic
    {
        #region Activation Checks

        public bool CanActivateAbility(GameplayAbility ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || ability == null)
                return false;

            if (spec.IsActive && !ability.canActivateWhileActive)
                return false;

            if (asc.IsAbilityOnCooldown(ability))
                return false;

            float abilityLevel = GetAbilityLevel(spec);

            float cost = ability.costAmount.GetValueAtLevel(abilityLevel, asc);
            if (cost > 0f)
            {
                var manaAttr = asc.AttributeSet?.GetAttribute(EGameplayAttributeType.Mana);
                if (manaAttr == null || manaAttr.CurrentValue < cost)
                {
                    return false;
                }
            }

            if (asc.HasAnyTags(ability.blockAbilitiesWithTags))
                return false;

            return true;
        }

        #endregion

        #region Activation

        public void ActivateAbility(GameplayAbility ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
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
        }

        private void ApplyCost(GameplayAbility ability, AbilitySystemComponent asc, float abilityLevel)
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

        private void ApplyCooldown(GameplayAbility ability, AbilitySystemComponent asc, float abilityLevel)
        {
            float cooldown = ability.cooldownDuration.GetValueAtLevel(abilityLevel, asc);
            if (cooldown > 0)
            {
                asc.StartCooldown(ability, cooldown);
            }
        }

        #endregion

        #region End/Cancel

        public void EndAbility(GameplayAbility ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || !spec.IsActive || ability == null)
                return;

            spec.SetActiveState(false);
            asc.RemoveTags(ability.abilityTags);
            asc.NotifyAbilityEnded(ability);
        }

        public void CancelAbility(GameplayAbility ability, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || !spec.IsActive || ability == null)
                return;

            // EndAbility will handle cleanup
            EndAbility(ability, asc, spec);
        }

        #endregion

        #region Effect Application

        public void ApplyEffectToTarget(
            GameplayEffect effect,
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            GameplayAbility ability,
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
            GameplayAbility ability,
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
