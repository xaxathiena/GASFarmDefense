using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Pure data container for AbilitySystemComponent state.
    /// No logic, only data storage with simple getters/setters.
    /// </summary>
    public class AbilitySystemData
    {
        // Owner
        public Transform Owner { get; set; }
        public string Id { get; set; }

        // Attributes
        public AttributeSet AttributeSet { get; set; }

        // Abilities
        public List<GameplayAbility> GrantedAbilities { get; set; } = new List<GameplayAbility>();
        public List<GameplayAbilitySpec> AbilitySpecs { get; set; } = new List<GameplayAbilitySpec>();
        public Dictionary<GameplayAbility, GameplayAbilitySpec> SpecLookup { get; set; } = new Dictionary<GameplayAbility, GameplayAbilitySpec>();
        public List<GameplayAbility> ActiveAbilities { get; set; } = new List<GameplayAbility>();

        // Tags (reference counting)
        public Dictionary<byte, int> ActiveTagCounts { get; set; } = new Dictionary<byte, int>();

        // Cooldowns
        public Dictionary<GameplayAbility, float> AbilityCooldowns { get; set; } = new Dictionary<GameplayAbility, float>();

        // Gameplay Effects
        public List<ActiveGameplayEffect> ActiveGameplayEffects { get; set; } = new List<ActiveGameplayEffect>();
    }
}
