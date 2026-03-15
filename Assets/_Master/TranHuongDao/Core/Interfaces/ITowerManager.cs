using System;
using System.Collections.Generic;
using GAS;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Manages fixed-position towers: placement validation, spawning, and tracking.
    /// Towers are stationary; their attack logic is delegated to GAS.
    /// </summary>
    public interface ITowerManager
    {
        // --- State ---

        /// <summary>Number of towers currently on the field.</summary>
        int ActiveTowerCount { get; }

        // --- Events ---

        /// <summary>Fired after a tower is successfully placed. Arg: instanceID.</summary>
        event Action<int> OnTowerPlaced;

        /// <summary>Fired after a tower is destroyed or sold. Arg: instanceID.</summary>
        event Action<int> OnTowerRemoved;

        // --- Commands ---

        /// <summary>
        /// Attempt to place a tower of <paramref name="towerID"/> at <paramref name="gridPosition"/>.
        /// Returns the unique instanceID if successful, -1 if the slot is occupied or invalid.
        /// </summary>
        int PlaceTower(string towerID, Vector3 gridPosition);

        /// <summary>Remove the tower with the given instanceID from the field.</summary>
        void RemoveTower(int instanceID);

        /// <summary>Return a read-only snapshot of all active tower instance IDs.</summary>
        IReadOnlyList<int> GetActiveTowerIDs();

        /// <summary>
        /// Return the world position of the tower with the given instanceID.
        /// Returns false if the tower does not exist.
        /// </summary>
        bool TryGetTowerPosition(int instanceID, out Vector3 position);

        /// <summary>
        /// Check whether a grid cell at <paramref name="gridPosition"/> is free to build on.
        /// </summary>
        bool IsCellAvailable(Vector3 gridPosition);

        /// <summary>
        /// Look up a live Tower object by its unique instance ID.
        /// Returns false if no tower with that ID is currently active.
        /// </summary>
        bool TryGetTower(int instanceID, out Tower tower);

        /// <summary>
        /// Retrieves the <see cref="GAS.AbilitySystemComponent"/> belonging to the tower
        /// with the given instanceID so abilities can apply <see cref="GAS.GameplayEffect"/>s.
        /// Returns false if the tower does not exist or has no ASC.
        /// </summary>
        bool TryGetTowerASC(int instanceID, out GAS.AbilitySystemComponent asc);

        /// <summary>
        /// Fill <paramref name="results"/> with the ASCs of all towers whose world
        /// position is within <paramref name="radius"/> of <paramref name="center"/>.
        /// At most <paramref name="maxCount"/> entries are added.
        /// </summary>
        void GetTowersInRange(Vector3 center, float radius, List<GAS.AbilitySystemComponent> ignoreList, List<GAS.AbilitySystemComponent> results, int maxCount = int.MaxValue);
    }
}
