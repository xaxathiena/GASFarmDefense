using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Defines when an ability should end after activation.
    /// </summary>
    public enum EAbilityEndPolicy
    {
        /// <summary>
        /// Ability ends immediately after OnActivated (fire-and-forget).
        /// Use for: Normal attacks, instant damage, instant heal.
        /// Cooldown starts immediately.
        /// </summary>
        InstantEnd = 0,
        
        /// <summary>
        /// Ability stays active until manually ended by calling EndAbility().
        /// Use for: Duration buffs, channeling spells, toggle abilities, auras.
        /// Cooldown only starts after EndAbility() is called.
        /// </summary>
        ManualEnd = 1
    }

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
        
        [Tooltip("InstantEnd: Auto-end after fire (normal attack, fireball).\nManualEnd: Stays active until EndAbility() called (buffs, channeling).")]
        public EAbilityEndPolicy endPolicy = EAbilityEndPolicy.InstantEnd;

        [Header("Tags")]
        public GameplayTag[] abilityTags;
        public GameplayTag[] cancelAbilitiesWithTags;
        public GameplayTag[] blockAbilitiesWithTags;
    }
}
