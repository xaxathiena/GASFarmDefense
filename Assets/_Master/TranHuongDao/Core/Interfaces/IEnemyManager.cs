using System;
using System.Collections.Generic;
using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Owns the full lifecycle of enemy units: spawn, update (pathfinding), and removal.
    /// Receives spawn commands from IWaveManager.
    /// </summary>
    public interface IEnemyManager
    {
        // --- State ---

        /// <summary>Number of enemies currently alive on the field.</summary>
        int ActiveEnemyCount { get; }

        // --- Events ---

        /// <summary>Fired after an enemy is successfully spawned. Arg: instanceID.</summary>
        event Action<int> OnEnemySpawned;

        /// <summary>
        /// Fired when an enemy is removed (reached the base or was killed).
        /// Arg: instanceID, reachedBase (true = leaked, false = killed).
        /// </summary>
        event Action<int, bool> OnEnemyRemoved;

        // --- Commands ---

        /// <summary>
        /// Spawn one enemy of the given type at <paramref name="spawnPoint"/>,
        /// following <paramref name="waypoints"/> toward the player base.
        /// Returns the unique instanceID assigned to the spawned enemy.
        /// </summary>
        int SpawnEnemy(string enemyID, Vector3 spawnPoint, IReadOnlyList<Vector3> waypoints);

        /// <summary>Remove (kill or despawn) the enemy with the given instanceID.</summary>
        void RemoveEnemy(int instanceID);

        /// <summary>Return a read-only snapshot of all active enemy instance IDs.</summary>
        IReadOnlyList<int> GetActiveEnemyIDs();

        /// <summary>
        /// Return the world position of the enemy with the given instanceID.
        /// Returns false if the enemy does not exist.
        /// </summary>
        bool TryGetEnemyPosition(int instanceID, out Vector3 position);

        // --- Combat helpers (used by tower ability behaviours) ---

        /// <summary>
        /// Returns the instanceID of the enemy closest to <paramref name="origin"/>
        /// within <paramref name="range"/>, or -1 if none exist in range.
        /// </summary>
        int GetClosestEnemyInRange(Vector3 origin, float range);

        /// <summary>
        /// Retrieves the <see cref="AbilitySystemComponent"/> belonging to the enemy
        /// with the given instanceID so abilities can apply <see cref="GAS.GameplayEffect"/>s.
        /// Returns false if the enemy does not exist or has no ASC.
        /// </summary>
        bool TryGetEnemyASC(int instanceID, out AbilitySystemComponent asc);

        /// <summary>
        /// Enqueue all spawn entries from <paramref name="config"/> and begin timed spawning
        /// using the supplied waypoint <paramref name="paths"/>.
        /// </summary>
        void BeginWave(WaveConfig config, IReadOnlyList<Vector3>[] paths);
    }
}
