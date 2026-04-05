using System;
using System.Collections.Generic;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    // CropDefinition
    //
    // Static data for one crop type (Carrot, Pumpkin, Grape, ...).
    // Authored in Unity Inspector via CropConfigSO.
    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    public class CropDefinition
    {
        [Tooltip("Unique identifier used in code/events. E.g. \"Carrot\"")]
        public string CropID;

        [Tooltip("Human-readable name for UI display.")]
        public string DisplayName;

        [Tooltip("Icon shown in FarmPopup and MarketPopup.")]
        public Sprite Icon;

        [Min(1), Tooltip("Number of waves that must pass before this crop is harvested.")]
        public int WavesToHarvest = 2;

        [Min(1), Tooltip("Minimum fruits yielded per seed planted.")]
        public int YieldMin = 5;

        [Min(1), Tooltip("Maximum fruits yielded per seed planted (inclusive).")]
        public int YieldMax = 10;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PlantedCrop
    //
    // Runtime record of one planting action. Stored in TDEconomyService and
    // checked each wave to determine if harvest is due.
    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    public struct PlantedCrop
    {
        /// <summary>Which crop type was planted.</summary>
        public string CropID;

        /// <summary>Wave index when the player planted these seeds.</summary>
        public int PlantedOnWave;

        /// <summary>Wave index at which this batch will be ready to harvest.</summary>
        public int HarvestOnWave; // PlantedOnWave + CropDefinition.WavesToHarvest

        /// <summary>How many seeds were planted in this single action.</summary>
        public int SeedCount;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CropConfigSO
    //
    // ScriptableObject asset. Drag into FarmRandomTDLifetimeScope inspector.
    // Contains all static balance data for the farming economic system.
    // ─────────────────────────────────────────────────────────────────────────
    [CreateAssetMenu(fileName = "CropConfig", menuName = "Map/RandomFarmTD/Crop Config")]
    public class CropConfigSO : BaseConfigSO
    {
        [Header("Crop Types")]
        public List<CropDefinition> crops = new List<CropDefinition>();

        [Header("Seed Economy")]
        [Min(1), Tooltip("Seeds granted to the player after each wave is cleared.")]
        public int SeedsPerWave = 10;

        [Header("Market Price Range (global — applies to all crop types)")]
        [Min(1)]
        public float MarketPriceMin = 50f;

        [Min(1)]
        public float MarketPriceMax = 200f;

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>O(n) lookup — call once at harvest time, not per-frame.</summary>
        public CropDefinition GetDefinition(string cropID)
        {
            foreach (var def in crops)
                if (def.CropID == cropID) return def;
            return null;
        }

        /// <summary>Returns true when cropID is a registered crop type.</summary>
        public bool IsValidCrop(string cropID) => GetDefinition(cropID) != null;
    }
}
