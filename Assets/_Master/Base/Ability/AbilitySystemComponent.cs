using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Master.Base.Ability
{
    /// <summary>
    /// Component that manages abilities for a GameObject (similar to UE's AbilitySystemComponent)
    /// </summary>
    public class AbilitySystemComponent : MonoBehaviour
    {
        [Header("Attribute Set")]
        [SerializeField] private AttributeSet attributeSetTemplate;
        private AttributeSet attributeSet;
        
        [Header("Resources")]
        [SerializeField] private float currentResource = 100f;
        [SerializeField] private float maxResource = 100f;
        
        [Header("Abilities")]
        [SerializeField] private List<GameplayAbility> grantedAbilities = new List<GameplayAbility>();
        
        // Runtime data
        private HashSet<string> activeTags = new HashSet<string>();
        private Dictionary<GameplayAbility, float> abilityCooldowns = new Dictionary<GameplayAbility, float>();
        private List<GameplayAbility> activeAbilities = new List<GameplayAbility>();
        private List<ActiveGameplayEffect> activeGameplayEffects = new List<ActiveGameplayEffect>();
        
        public float CurrentResource => currentResource;
        public float MaxResource => maxResource;
        public AttributeSet AttributeSet => attributeSet;
        
        private void Start()
        {
            InitializeAttributeSet();
        }
        
        private void Update()
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
        }
        
        /// <summary>
        /// Grant an ability to this component
        /// </summary>
        public void GiveAbility(GameplayAbility ability)
        {
            if (!grantedAbilities.Contains(ability))
                grantedAbilities.Add(ability);
        }
        
        /// <summary>
        /// Try to activate an ability
        /// </summary>
        public bool TryActivateAbility(GameplayAbility ability)
        {
            if (!grantedAbilities.Contains(ability))
            {
                Debug.LogWarning($"Ability {ability.abilityName} is not granted to {gameObject.name}");
                return false;
            }
            
            if (ability.CanActivateAbility(this))
            {
                ability.ActivateAbility(this);
                activeAbilities.Add(ability);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Try to activate an ability by index
        /// </summary>
        public bool TryActivateAbilityByIndex(int index)
        {
            if (index < 0 || index >= grantedAbilities.Count)
                return false;
            
            return TryActivateAbility(grantedAbilities[index]);
        }
        
        /// <summary>
        /// Cancel an ability
        /// </summary>
        public void CancelAbility(GameplayAbility ability)
        {
            if (activeAbilities.Contains(ability))
            {
                ability.CancelAbility();
                activeAbilities.Remove(ability);
            }
        }
        
        /// <summary>
        /// Cancel abilities that have any of the specified tags
        /// </summary>
        public void CancelAbilitiesWithTags(string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return;
            
            var abilitiesToCancel = activeAbilities
                .Where(a => a.abilityTags != null && a.abilityTags.Any(tag => tags.Contains(tag)))
                .ToList();
            
            foreach (var ability in abilitiesToCancel)
            {
                CancelAbility(ability);
            }
        }
        
        #region Tags
        
        /// <summary>
        /// Add gameplay tags
        /// </summary>
        public void AddTags(params string[] tags)
        {
            if (tags == null)
                return;
            
            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag))
                    activeTags.Add(tag);
            }
        }
        
        /// <summary>
        /// Remove gameplay tags
        /// </summary>
        public void RemoveTags(params string[] tags)
        {
            if (tags == null)
                return;
            
            foreach (var tag in tags)
            {
                activeTags.Remove(tag);
            }
        }
        
        /// <summary>
        /// Check if has any of the specified tags
        /// </summary>
        public bool HasAnyTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;
            
            return tags.Any(tag => activeTags.Contains(tag));
        }
        
        /// <summary>
        /// Check if has all of the specified tags
        /// </summary>
        public bool HasAllTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;
            
            return tags.All(tag => activeTags.Contains(tag));
        }
        
        #endregion
        
        #region Attribute Set
        
        /// <summary>
        /// Initialize attribute set
        /// </summary>
        private void InitializeAttributeSet()
        {
            if (attributeSetTemplate != null)
            {
                // Create instance of attribute set
                attributeSet = Instantiate(attributeSetTemplate);
                attributeSet.InitAttributeSet(this);
            }
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
            return abilityCooldowns.ContainsKey(ability) && abilityCooldowns[ability] > 0;
        }
        
        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetAbilityCooldownRemaining(GameplayAbility ability)
        {
            if (abilityCooldowns.TryGetValue(ability, out float remaining))
                return remaining;
            return 0f;
        }
        
        #endregion
        
        #region Resources
        
        /// <summary>
        /// Check if has enough resource
        /// </summary>
        public bool HasEnoughResource(float amount)
        {
            return currentResource >= amount;
        }
        
        /// <summary>
        /// Consume resource
        /// </summary>
        public void ConsumeResource(float amount)
        {
            currentResource = Mathf.Max(0, currentResource - amount);
        }
        
        /// <summary>
        /// Add resource
        /// </summary>
        public void AddResource(float amount)
        {
            currentResource = Mathf.Min(maxResource, currentResource + amount);
        }
        
        /// <summary>
        /// Set resource
        /// </summary>
        public void SetResource(float amount)
        {
            currentResource = Mathf.Clamp(amount, 0, maxResource);
        }
        
        #endregion
        
        #region Gameplay Effects
        
        /// <summary>
        /// Apply a gameplay effect to this component
        /// </summary>
        public ActiveGameplayEffect ApplyGameplayEffectToSelf(GameplayEffect effect, AbilitySystemComponent source = null)
        {
            return ApplyGameplayEffectToTarget(effect, this, source ?? this);
        }
        
        /// <summary>
        /// Apply a gameplay effect to a target
        /// </summary>
        public ActiveGameplayEffect ApplyGameplayEffectToTarget(GameplayEffect effect, AbilitySystemComponent target, AbilitySystemComponent source)
        {
            if (effect == null || target == null)
                return null;
            
            // Check if effect can be applied
            if (!effect.CanApplyTo(target))
            {
                Debug.LogWarning($"Cannot apply {effect.effectName} to {target.gameObject.name}");
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
                        Debug.Log($"Stacked {effect.effectName} on {target.gameObject.name} (x{existingEffect.StackCount})");
                        return existingEffect;
                    }
                }
            }
            
            // Create active effect
            var activeEffect = new ActiveGameplayEffect(effect, source, target);
            
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
                    effect.ApplyModifiers(target.AttributeSet, activeEffect.StackCount);
                }
                
                Debug.Log($"Applied instant effect {effect.effectName} to {target.gameObject.name}");
                return activeEffect; // Don't add to active list
            }
            
            // Apply initial modifiers for non-periodic duration/infinite effects
            if (!effect.isPeriodic && target.AttributeSet != null)
            {
                effect.ApplyModifiers(target.AttributeSet, activeEffect.StackCount);
            }
            
            // Subscribe to expiration
            activeEffect.OnEffectExpired += OnGameplayEffectExpired;
            activeEffect.OnEffectRemoved += OnGameplayEffectRemoved;
            
            // Add to active effects
            target.activeGameplayEffects.Add(activeEffect);
            
            Debug.Log($"Applied {effect.effectName} to {target.gameObject.name} (Duration: {activeEffect.Duration}s)");
            return activeEffect;
        }
        
        /// <summary>
        /// Remove a specific active gameplay effect
        /// </summary>
        public void RemoveGameplayEffect(ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null || !activeGameplayEffects.Contains(activeEffect))
                return;
            
            // Remove tags
            if (activeEffect.Effect.grantedTags != null)
            {
                RemoveTags(activeEffect.Effect.grantedTags);
            }
            
            activeGameplayEffects.Remove(activeEffect);
            Debug.Log($"Removed {activeEffect.Effect.effectName} from {gameObject.name}");
        }
        
        /// <summary>
        /// Remove all gameplay effects with specific tags
        /// </summary>
        public void RemoveGameplayEffectsWithTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return;
            
            var effectsToRemove = activeGameplayEffects
                .Where(e => e.Effect.grantedTags != null && e.Effect.grantedTags.Any(tag => tags.Contains(tag)))
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
