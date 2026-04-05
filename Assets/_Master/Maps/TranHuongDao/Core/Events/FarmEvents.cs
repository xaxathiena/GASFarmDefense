using System.Collections.Generic;

namespace Abel.TranHuongDao.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    // Farm Events
    //
    // All events related to the farming economic system.
    // Published via FD.IEventBus — no MonoBehaviour required.
    //
    // Consumers:
    //   FarmPopup        — SeedsGrantedEvent, CropHarvestedEvent, CropPlantedEvent
    //   MarketPopup      — MarketPricesUpdatedEvent, CropSoldEvent
    //   HUD              — SeedsGrantedEvent (seed count display)
    //   NotificationUI   — CropHarvestedEvent (toast popup)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired after a wave is completed — signals the player has received new seeds.
    /// </summary>
    public readonly struct SeedsGrantedEvent
    {
        /// <summary>Number of seeds that were added to the player's stockpile.</summary>
        public readonly int Count;

        /// <summary>Total seeds the player now holds (after the grant).</summary>
        public readonly int TotalSeeds;

        public SeedsGrantedEvent(int count, int totalSeeds)
        {
            Count      = count;
            TotalSeeds = totalSeeds;
        }
    }

    /// <summary>
    /// Fired once per PlantedCrop batch when it reaches its HarvestOnWave.
    /// Multiple events may fire in rapid succession at wave start if several batches ripen.
    /// </summary>
    public readonly struct CropHarvestedEvent
    {
        public readonly string CropID;

        /// <summary>Total fruits produced by this batch (already added to Inventory).</summary>
        public readonly int Yield;

        public CropHarvestedEvent(string cropID, int yield)
        {
            CropID = cropID;
            Yield  = yield;
        }
    }

    /// <summary>
    /// Fired at the start of every wave with the fresh randomised prices.
    /// UI should refresh all price displays when this arrives.
    /// </summary>
    public readonly struct MarketPricesUpdatedEvent
    {
        /// <summary>Snapshot of current prices keyed by CropID.</summary>
        public readonly IReadOnlyDictionary<string, float> Prices;

        public MarketPricesUpdatedEvent(IReadOnlyDictionary<string, float> prices)
        {
            Prices = prices;
        }
    }

    /// <summary>Fired when the player successfully sells crops for gold.</summary>
    public readonly struct CropSoldEvent
    {
        public readonly string CropID;
        public readonly int    Quantity;
        public readonly int    GoldEarned;

        public CropSoldEvent(string cropID, int quantity, int goldEarned)
        {
            CropID     = cropID;
            Quantity   = quantity;
            GoldEarned = goldEarned;
        }
    }

    /// <summary>Fired when the player plants seeds.</summary>
    public readonly struct CropPlantedEvent
    {
        public readonly string CropID;
        public readonly int    SeedCount;

        /// <summary>Wave index at which this batch will ripen.</summary>
        public readonly int    HarvestOnWave;

        public CropPlantedEvent(string cropID, int seedCount, int harvestOnWave)
        {
            CropID        = cropID;
            SeedCount     = seedCount;
            HarvestOnWave = harvestOnWave;
        }
    }
}
