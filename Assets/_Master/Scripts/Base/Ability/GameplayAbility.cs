using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Base class for all gameplay abilities (similar to UE's GameplayAbility)
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameplayAbility", menuName = "GAS/Base/Gameplay Ability")]
    public class GameplayAbility : ScriptableObject
    {
        [Header("Ability Info")]
        public string abilityName;
        public string description;

        [Header("Ability Properties")]
        public ScalableFloat cooldownDuration = new ScalableFloat();
        public ScalableFloat costAmount = new ScalableFloat();
        public bool canActivateWhileActive = false;

        [Header("Tags")]
        public string[] abilityTags;
        public string[] cancelAbilitiesWithTags;
        public string[] blockAbilitiesWithTags;

        /// <summary>
        /// Check if the ability can be activated using its resolved spec.
        /// </summary>
        public bool CanActivateAbility(AbilitySystemComponent asc)
        {
            var spec = asc?.GetAbilitySpec(this);
            return CanActivateAbility(asc, spec);
        }

        public virtual bool CanActivateAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null)
                return false;

            if (spec.IsActive && !canActivateWhileActive)
                return false;

            if (asc.IsAbilityOnCooldown(this))
                return false;

            float abilityLevel = GetAbilityLevel(spec);

            float cost = costAmount.GetValueAtLevel(abilityLevel, asc);
            if (cost > 0f)
            {
                var manaAttr = asc.AttributeSet?.GetAttribute(EGameplayAttributeType.Mana);
                if (manaAttr == null || manaAttr.CurrentValue < cost)
                {
                    return false;
                }
            }

            if (asc.HasAnyTags(blockAbilitiesWithTags))
                return false;

            return true;
        }

        /// <summary>
        /// Activate the ability.
        /// </summary>
        public void ActivateAbility(AbilitySystemComponent asc)
        {
            var spec = asc?.GetAbilitySpec(this);
            ActivateAbility(asc, spec);
        }

        public virtual void ActivateAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (!CanActivateAbility(asc, spec))
                return;

            spec.SetActiveState(true);

            asc.CancelAbilitiesWithTags(cancelAbilitiesWithTags);
            asc.AddTags(abilityTags);

            float abilityLevel = GetAbilityLevel(spec);

            float cost = costAmount.GetValueAtLevel(abilityLevel, asc);
            if (cost > 0f && asc.AttributeSet != null)
            {
                var manaAttr = asc.AttributeSet.GetAttribute(EGameplayAttributeType.Mana);
                if (manaAttr != null)
                {
                    manaAttr.ModifyCurrentValue(-cost);
                }
            }

            float cooldown = cooldownDuration.GetValueAtLevel(abilityLevel, asc);
            if (cooldown > 0)
            {
                asc.StartCooldown(this, cooldown);
            }

            OnAbilityActivated(asc, spec);
        }

        /// <summary>
        /// Override this to implement ability-specific logic.
        /// </summary>
        protected virtual void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
        }

        public void EndAbility(AbilitySystemComponent asc)
        {
            var spec = asc?.GetAbilitySpec(this);
            EndAbility(asc, spec);
        }

        public virtual void EndAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || !spec.IsActive)
                return;

            spec.SetActiveState(false);
            asc.RemoveTags(abilityTags);
            asc.NotifyAbilityEnded(this);

            OnAbilityEnded(asc, spec);
        }

        protected virtual void OnAbilityEnded(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
        }

        public virtual void CancelAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || !spec.IsActive)
                return;

            OnAbilityCancelled(asc, spec);
            EndAbility(asc, spec);
        }

        protected virtual void OnAbilityCancelled(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
        }

        protected float GetAbilityLevel(AbilitySystemComponent asc)
        {
            if (asc == null)
                return 1f;

            var spec = asc.GetAbilitySpec(this);
            return GetAbilityLevel(spec);
        }

        protected float GetAbilityLevel(GameplayAbilitySpec spec)
        {
            return spec?.Level ?? 1f;
        }

        protected GameObject GetAbilityOwner(AbilitySystemComponent asc)
        {
            return asc?.GetOwner();
        }
    }
}
