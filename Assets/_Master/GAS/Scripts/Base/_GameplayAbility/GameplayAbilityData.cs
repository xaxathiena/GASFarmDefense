using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Base class for ability data - PURE DATA ONLY, NO LOGIC.
    /// Contains only configuration properties.
    /// All logic including behaviour type resolution is in GameplayAbilityLogic.
    /// </summary>
    public abstract class GameplayAbilityData : ScriptableObject
    {
        [Header("Ability Info")]
        public string abilityName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Ability Properties")]
        public ScalableFloat cooldownDuration = new ScalableFloat();
        public ScalableFloat costAmount = new ScalableFloat();
        public bool canActivateWhileActive = false;

        [Header("Tags")]
        public GameplayTag[] abilityTags;
        public GameplayTag[] cancelAbilitiesWithTags;
        public GameplayTag[] blockAbilitiesWithTags;
    }
}
