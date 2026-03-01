using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Holds the configuration data for the random tower building system.
    /// Designed as a plain serializable class (DOD-style) so it can be embedded
    /// inside a ScriptableObject or a MonoBehaviour without any Unity lifecycle coupling.
    /// </summary>
    [Serializable]
    public class TowerBuilderConfig
    {
        // ── Data ─────────────────────────────────────────────────────────────────

        [Tooltip("List of tower IDs available for random selection (e.g. Tower_Archer, Tower_Mage).")]
        public List<string> availableTowerIDs = new List<string>
        {
            "Tower_Archer",
            "Tower_Mage"
        };
        private Dictionary<int, List<string>> tierToTowerIDs  = new Dictionary<int, List<string>>();
        // ── Query ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a uniformly random tower ID from <see cref="availableTowerIDs"/>.
        /// Throws <see cref="InvalidOperationException"/> when the list is empty.
        /// </summary>
        public string GetRandomTowerID()
        {
            if (availableTowerIDs == null || availableTowerIDs.Count == 0)
                throw new InvalidOperationException(
                    "[TowerBuilderConfig] availableTowerIDs is empty. " +
                    "Add at least one tower ID before calling GetRandomTowerID().");

            // Unity's Random.Range upper bound is exclusive for integers, so
            // passing Count gives an evenly distributed pick across all elements.
            int index = UnityEngine.Random.Range(0, availableTowerIDs.Count);
            return availableTowerIDs[index];
        }
        public string GetTowerIDWithTier(int tier, UnitsConfig unitsConfig)
        {
            if (availableTowerIDs == null || availableTowerIDs.Count == 0)
                throw new InvalidOperationException(
                    "[TowerBuilderConfig] availableTowerIDs is empty. " +
                    "Add at least one tower ID before calling GetTowerIDWithTier().");

            // Unity's Random.Range upper bound is exclusive for integers, so
            // passing Count gives an evenly distributed pick across all elements.
            if(tierToTowerIDs.TryGetValue(tier, out var cachedList))
            {
                if (cachedList.Count == 0)
                {
                    Debug.LogError($"[TowerBuilderConfig] No tower IDs found for tier {tier} in config!");
                    return "";
                }
                int tierIndex = UnityEngine.Random.Range(0, cachedList.Count);
                return cachedList[tierIndex];
            }
            List<string> filteredTowerIDs = new List<string>();
            foreach (var item in availableTowerIDs)
            {
                unitsConfig.TryGetConfig(item, out var config);
                if (config.Tier == tier)
                {
                    filteredTowerIDs.Add(item);
                }
            }
            if (filteredTowerIDs.Count == 0)
            {
                return "";
            }

            int index = UnityEngine.Random.Range(0, filteredTowerIDs.Count);
            tierToTowerIDs[tier] = filteredTowerIDs;
            return filteredTowerIDs[index];
        }
    }
}
