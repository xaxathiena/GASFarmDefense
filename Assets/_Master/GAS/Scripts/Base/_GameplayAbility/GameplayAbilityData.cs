using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Base class for ability data (configuration only, no logic).
    /// Each ability type inherits from this and specifies its behaviour type.
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

        /// <summary>
        /// Returns the Type of the behaviour that handles this ability's logic.
        /// Must be overridden by subclasses to return their specific behaviour type.
        /// </summary>
        public abstract System.Type GetBehaviourType();
    }
}
