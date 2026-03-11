
namespace GAS
{
    /// <summary>
    /// Event broadcasted whenever a GameplayTag's stack count changes on any AbilitySystemData.
    /// Used by VFX, UI, and Audio to react to buffs/debuffs decoupling from core GAS.
    /// </summary>
    public readonly struct GameplayTagChangedEvent
    {
        /// <summary>
        /// The GameObject Instance ID of the Owner.
        /// </summary>
        public readonly int OwnerInstanceID;

        /// <summary>
        /// The tag that was added or removed.
        /// </summary>
        public readonly GameplayTag Tag;

        /// <summary>
        /// The new stack count of the tag. 0 means the tag is fully removed.
        /// </summary>
        public readonly int NewCount;

        public GameplayTagChangedEvent(int ownerInstanceID, GameplayTag tag, int newCount)
        {
            OwnerInstanceID = ownerInstanceID;
            Tag = tag;
            NewCount = newCount;
        }
    }
}
