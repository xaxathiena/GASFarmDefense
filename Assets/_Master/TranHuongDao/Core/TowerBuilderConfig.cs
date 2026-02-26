using System;
using System.Collections.Generic;
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
    }
}
