using System.Collections.Generic;
using UnityEngine;

namespace FD.Character
{
    /// <summary>
    /// Centralized manager for tracking all active enemies in the scene.
    /// Provides efficient distance-based queries without Physics overhead.
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        private static EnemyManager _instance;
        private static readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>(100);
        private static readonly List<EnemyBase> _queryResultBuffer = new List<EnemyBase>(50);
        
        // Reusable transform list to avoid allocations
        private static readonly List<Transform> _transformResultBuffer = new List<Transform>(50);

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Register an enemy when it becomes active
        /// </summary>
        public static void RegisterEnemy(EnemyBase enemy)
        {
            if (enemy == null || _activeEnemies.Contains(enemy))
            {
                return;
            }

            _activeEnemies.Add(enemy);
        }

        /// <summary>
        /// Unregister an enemy when it becomes inactive or destroyed
        /// </summary>
        public static void UnregisterEnemy(EnemyBase enemy)
        {
            if (enemy == null)
            {
                return;
            }

            _activeEnemies.Remove(enemy);
        }

        /// <summary>
        /// Get all enemies within range of a position, filtered by layer mask.
        /// Uses squared distance for performance (no sqrt).
        /// </summary>
        /// <param name="position">Center position to search from</param>
        /// <param name="range">Maximum distance</param>
        /// <param name="layerMask">Layer mask to filter enemies</param>
        /// <returns>List of enemy transforms within range</returns>
        public static List<Transform> GetEnemiesInRange(Vector3 position, float range, LayerMask layerMask)
        {
            _queryResultBuffer.Clear();
            _transformResultBuffer.Clear();

            if (range <= 0f)
            {
                return _transformResultBuffer;
            }

            float rangeSqr = range * range;

            // Iterate through all active enemies
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];

                // Clean up null references
                if (enemy == null || enemy.gameObject == null)
                {
                    _activeEnemies.RemoveAt(i);
                    continue;
                }

                // Skip inactive enemies
                if (!enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // Check layer mask
                int enemyLayer = enemy.gameObject.layer;
                if ((layerMask.value & (1 << enemyLayer)) == 0)
                {
                    continue;
                }

                // Check distance using squared magnitude (faster than distance)
                float distanceSqr = (enemy.transform.position - position).sqrMagnitude;
                if (distanceSqr <= rangeSqr)
                {
                    _queryResultBuffer.Add(enemy);
                }
            }

            // Convert to transform list (reuse buffer)
            for (int i = 0; i < _queryResultBuffer.Count; i++)
            {
                _transformResultBuffer.Add(_queryResultBuffer[i].transform);
            }

            return _transformResultBuffer;
        }

        /// <summary>
        /// Get count of active enemies (for debugging)
        /// </summary>
        public static int GetActiveEnemyCount()
        {
            // Clean up null references
            _activeEnemies.RemoveAll(e => e == null);
            return _activeEnemies.Count;
        }

        /// <summary>
        /// Clear all registered enemies (useful for scene changes)
        /// </summary>
        public static void ClearAll()
        {
            _activeEnemies.Clear();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                ClearAll();
            }
        }
    }
}
