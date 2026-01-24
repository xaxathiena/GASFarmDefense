using UnityEngine;

namespace _Master.Base.Ability
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
        
        [Header("Level")]
        [Tooltip("Current level of this ability instance")]
        public float abilityLevel = 1f;
        
        [Header("Tags")]
        public string[] abilityTags;
        public string[] cancelAbilitiesWithTags;
        public string[] blockAbilitiesWithTags;
        
        protected AbilitySystemComponent ownerASC;
        protected GameObject owner;
        protected bool isActive = false;
        
        /// <summary>
        /// Check if the ability can be activated
        /// </summary>
        public virtual bool CanActivateAbility(AbilitySystemComponent asc)
        {
            ownerASC = asc;
            owner = asc.GetOwner();
            
            // Check if already active
            if (isActive && !canActivateWhileActive)
                return false;
            
            // Check cooldown
            if (asc.IsAbilityOnCooldown(this))
                return false;
            
            // Check cost (Mana from AttributeSet)
            float cost = costAmount.GetValueAtLevel(abilityLevel, asc);
            if (cost > 0f)
            {
                var manaAttr = asc.AttributeSet?.GetAttribute(EGameplayAttributeType.Mana);
                if (manaAttr == null || manaAttr.CurrentValue < cost)
                {
                    return false;
                }
            }
            
            // Check blocked tags
            if (asc.HasAnyTags(blockAbilitiesWithTags))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Activate the ability
        /// </summary>
        public virtual void ActivateAbility(AbilitySystemComponent asc)
        {
            if (!CanActivateAbility(asc))
                return;
            
            ownerASC = asc;
            owner = asc.GetOwner();
            isActive = true;
            
            // Cancel conflicting abilities
            asc.CancelAbilitiesWithTags(cancelAbilitiesWithTags);
            
            // Add ability tags
            asc.AddTags(abilityTags);
            
            // Consume cost (Mana from AttributeSet)
            float cost = costAmount.GetValueAtLevel(abilityLevel, ownerASC);
            if (cost > 0f && ownerASC.AttributeSet != null)
            {
                var manaAttr = ownerASC.AttributeSet.GetAttribute(EGameplayAttributeType.Mana);
                if (manaAttr != null)
                {
                    manaAttr.ModifyCurrentValue(-cost);
                }
            }
            
            // Start cooldown
            float cooldown = cooldownDuration.GetValueAtLevel(abilityLevel, ownerASC);
            if (cooldown > 0)
                asc.StartCooldown(this, cooldown);
            
            // Execute ability logic
            OnAbilityActivated();
        }
        
        /// <summary>
        /// Override this to implement ability logic
        /// </summary>
        protected virtual void OnAbilityActivated()
        {
        }
        
        /// <summary>
        /// End the ability
        /// </summary>
        public virtual void EndAbility()
        {
            if (!isActive)
                return;
            
            isActive = false;
            
            // Remove ability tags
            if (ownerASC != null)
                ownerASC.RemoveTags(abilityTags);
            
            OnAbilityEnded();
        }
        
        /// <summary>
        /// Override this for cleanup logic
        /// </summary>
        protected virtual void OnAbilityEnded()
        {
        }
        
        /// <summary>
        /// Cancel the ability
        /// </summary>
        public virtual void CancelAbility()
        {
            if (!isActive)
                return;
            
            OnAbilityCancelled();
            EndAbility();
        }
        
        /// <summary>
        /// Override this for cancel logic
        /// </summary>
        protected virtual void OnAbilityCancelled()
        {
        }
    }
}
