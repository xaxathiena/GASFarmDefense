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
        public List<GameplayAbilityData> GrantedAbilities { get; set; } = new List<GameplayAbilityData>();
        public List<GameplayAbilitySpec> AbilitySpecs { get; set; } = new List<GameplayAbilitySpec>();
        public Dictionary<GameplayAbilityData, GameplayAbilitySpec> SpecLookup { get; set; } = new Dictionary<GameplayAbilityData, GameplayAbilitySpec>();
        public List<GameplayAbilityData> ActiveAbilities { get; set; } = new List<GameplayAbilityData>();

        // Tags (reference counting)
        public Dictionary<byte, int> ActiveTagCounts { get; set; } = new Dictionary<byte, int>();

        // Cooldowns
        public Dictionary<GameplayAbilityData, float> AbilityCooldowns { get; set; } = new Dictionary<GameplayAbilityData, float>();

        // Gameplay Effects
        public List<ActiveGameplayEffect> ActiveGameplayEffects { get; set; } = new List<ActiveGameplayEffect>();
    }
}
