using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Defines when an ability should be activated.
    /// </summary>
    public enum EAbilityActivationPolicy
    {
        /// <summary>
        /// Ability is activated on demand (e.g. by player input or AI logic ticking).
        /// </summary>
        OnDemand = 0,

        /// <summary>
        /// Ability is automatically activated immediately when granted to the ASC. Use for passive abilities or auras.
        /// </summary>
        OnGranted = 1
    }

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
        public string abilityID; // Unique string ID for this ability, used for lookups. Can be auto-generated or manually assigned.
        [Header("Ability Info")]
        public string abilityName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Ability Properties")]
        [Tooltip("OnDemand: Triggered manually or by AI logic tick.\nOnGranted: Auto-activated when given to character (for Passives/Aura).")]
        public EAbilityActivationPolicy activationPolicy = EAbilityActivationPolicy.OnDemand;

        public float cooldownDuration = 0f;
        public ScalableFloat costAmount = new ScalableFloat();

        [Tooltip("InstantEnd: Auto-end after fire (normal attack, fireball).\nManualEnd: Stays active until EndAbility() called (buffs, channeling).")]
        public EAbilityEndPolicy endPolicy = EAbilityEndPolicy.InstantEnd;

        [Header("Tags")]
        public GameplayTag[] abilityTags;
        public GameplayTag[] cancelAbilitiesWithTags;
        public GameplayTag[] blockAbilitiesWithTags;
    }
}
