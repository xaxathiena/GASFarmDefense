using System.Collections.Generic;
using FD.Views;
using UnityEngine;
using VContainer.Unity;

namespace FD
{
    /// <summary>
    /// VContainer service for managing all active enemies
    /// Replaces old singleton MonoBehaviour EnemyManager
    /// </summary>
    public class EnemyManager : IStartable
    {
        private readonly List<EnemyController> activeEnemies = new List<EnemyController>(100);
        private readonly IDebugService debug;
        
        // Reusable buffers to avoid allocations
        private readonly List<EnemyController> queryResultBuffer = new List<EnemyController>(50);
        private readonly List<Transform> transformResultBuffer = new List<Transform>(50);

        public EnemyManager(IDebugService debug)
        {
            this.debug = debug;
        }

        public void Start()
        {
            debug.Log("EnemyManager service initialized!", Color.green);
        }

        /// <summary>
        /// Register an enemy controller when it becomes active
        /// </summary>
        public void RegisterEnemy(EnemyController enemy)
        {
            if (enemy == null || activeEnemies.Contains(enemy))
            {
                return;
            }

            activeEnemies.Add(enemy);
            debug.Log($"Enemy {enemy.Id} registered. Total enemies: {activeEnemies.Count}", Color.cyan);
        }

        /// <summary>
        /// Unregister an enemy controller when it becomes inactive or destroyed
        /// </summary>
        public void UnregisterEnemy(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (activeEnemies.Remove(enemy))
            {
                debug.Log($"Enemy {enemy.Id} unregistered. Total enemies: {activeEnemies.Count}", Color.yellow);
            }
        }

        /// <summary>
        /// Get all enemies within range of a position, filtered by layer mask.
        /// Uses squared distance for performance (no sqrt).
        /// </summary>
        public List<Transform> GetEnemiesInRange(Vector3 position, float range, LayerMask layerMask)
        {
            queryResultBuffer.Clear();
            transformResultBuffer.Clear();

            if (range <= 0f)
            {
                return transformResultBuffer;
            }

            float rangeSqr = range * range;

            // Clean up null/inactive enemies while iterating
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = activeEnemies[i];

                // Clean up null references
                if (enemy == null || enemy.GameObject == null)
                {
                    activeEnemies.RemoveAt(i);
                    continue;
                }

                // Skip inactive enemies
                if (!enemy.IsActive)
                {
                    continue;
                }

                // Check layer mask
                int enemyLayer = enemy.Layer;
                if ((layerMask.value & (1 << enemyLayer)) == 0)
                {
                    continue;
                }

                // Check distance using squared magnitude (faster than distance)
                float distanceSqr = (enemy.Position - position).sqrMagnitude;
                if (distanceSqr <= rangeSqr)
                {
                    queryResultBuffer.Add(enemy);
                }
            }

            // Convert to transform list (reuse buffer)
            for (int i = 0; i < queryResultBuffer.Count; i++)
            {
                transformResultBuffer.Add(queryResultBuffer[i].Transform);
            }

            return transformResultBuffer;
        }

        /// <summary>
        /// Get count of active enemies (for debugging)
        /// </summary>
        public int GetActiveEnemyCount()
        {
            // Clean up null references
            activeEnemies.RemoveAll(e => e == null);
            return activeEnemies.Count;
        }

        /// <summary>
        /// Get all active enemy controllers
        /// </summary>
        public List<EnemyController> GetAllActiveEnemies()
        {
            return activeEnemies;
        }

        /// <summary>
        /// Clear all registered enemies (useful for scene changes)
        /// </summary>
        public void ClearAll()
        {
            activeEnemies.Clear();
            debug.Log("All enemies cleared!", Color.yellow);
        }
    }
}
