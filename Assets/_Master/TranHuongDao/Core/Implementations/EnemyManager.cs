using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using GAS;
using Enemy = Abel.TranHuongDao.Core.Enemy;
namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Manages the full lifecycle of enemies:
    ///   • Reads a <see cref="WaveConfig"/> and spawns enemies over time using an internal queue.
    ///   • Calls <see cref="IRender2DService"/> to update / remove visuals.
    ///   • Notifies <see cref="IWaveManager"/> when an enemy is defeated.
    ///
    /// Runs as a VContainer Singleton + ITickable (no MonoBehaviour required).
    /// </summary>
    public class EnemyManager : IEnemyManager, ITickable, IStartable, IDisposable
    {
        // ── Dependencies ─────────────────────────────────────────────────────────
        private readonly IObjectResolver  container;      // resolves Transient ASC per enemy
        private readonly IRender2DService renderService;
        /// <summary>
        /// Service for generating unique instance IDs for enemies.  Used for render mapping and GAS targeting.
        /// </summary>
        private readonly IInstanceIDService instanceIDService; // generates unique instance IDs for enemies

        // ── Active enemies ────────────────────────────────────────────────────────
        private readonly Dictionary<int, Enemy> activeEnemies = new Dictionary<int, Enemy>(128);
        private readonly List<int>              activeIDBuffer = new List<int>(128);
        // Enemies that died this frame – removed after the Tick loop
        private readonly List<Enemy>            pendingRemoval = new List<Enemy>(16);

        // ── IEnemyManager events ─────────────────────────────────────────────────
        public event Action<int>       OnEnemySpawned;
        public event Action<int, bool> OnEnemyRemoved;   // (instanceID, reachedBase)

        // ── Active count ─────────────────────────────────────────────────────────
        public int ActiveEnemyCount => activeEnemies.Count;


        // ── Spawn queue ──────────────────────────────────────────────────────────
        // Sorted by delayRemaining ascending; we consume from index 0.
        private readonly List<SpawnQueueEntry> spawnQueue = new List<SpawnQueueEntry>(64);
        private bool isSpawning;

        private struct SpawnQueueEntry
        {
            public string                 enemyID;
            public float                  maxHealth;
            public float                  moveSpeed;
            public IReadOnlyList<Vector3> waypoints;
            public float                  delayRemaining;   // seconds until this unit spawns
        }

        // ─────────────────────────────────────────────────────────────────────────
        public EnemyManager(
            IObjectResolver  container,
            IRender2DService renderService, IInstanceIDService instanceIDService)
        {
            this.container     = container;
            this.renderService = renderService;
            this.instanceIDService = instanceIDService;
        }

        // ── VContainer entry points ──────────────────────────────────────────────

        public void Start()
        {
            Debug.Log("[EnemyManager] Initialized.");
        }

        public void Dispose()
        {
            var survivors = new List<Enemy>(activeEnemies.Values);
            foreach (var enemy in survivors)
                KillEnemy(enemy, reachedBase: false, notify: false);

            activeEnemies.Clear();
            spawnQueue.Clear();
        }

        // ── ITickable ────────────────────────────────────────────────────────────

        /// <summary>Called every frame by VContainer's PlayerLoopRunner.</summary>
        public void Tick()
        {
            float dt = Time.deltaTime;

            ProcessSpawnQueue(dt);
            TickActiveEnemies(dt);
            FlushPendingRemovals();
        }

        // ── IEnemyManager ────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately spawn one enemy at the start of <paramref name="waypoints"/>.
        /// Returns the assigned instanceID.
        /// </summary>
        public int SpawnEnemy(string enemyID, Vector3 spawnPoint, IReadOnlyList<Vector3> waypoints)
        {
            return SpawnEnemyInternal(enemyID, 100f, .1f, waypoints);
        }

        public void RemoveEnemy(int instanceID)
        {
            if (activeEnemies.TryGetValue(instanceID, out var enemy))
                KillEnemy(enemy, reachedBase: false, notify: true);
        }

        public IReadOnlyList<int> GetActiveEnemyIDs()
        {
            activeIDBuffer.Clear();
            activeIDBuffer.AddRange(activeEnemies.Keys);
            return activeIDBuffer;
        }

        public bool TryGetEnemyPosition(int instanceID, out Vector3 position)
        {
            if (activeEnemies.TryGetValue(instanceID, out var enemy))
            {
                position = enemy.Position;
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        public int GetClosestEnemyInRange(Vector3 origin, float range)
        {
            float rangeSqr  = range * range;
            float minDistSqr = float.MaxValue;
            int   bestID    = -1;

            foreach (var kvp in activeEnemies)
            {
                var enemy = kvp.Value;
                if (!enemy.IsAlive || enemy.HasReachedEnd) continue;

                float distSqr = (enemy.Position - origin).sqrMagnitude;
                if (distSqr <= rangeSqr && distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    bestID     = kvp.Key;
                }
            }

            return bestID;
        }

        public bool TryGetEnemyASC(int instanceID, out GAS.AbilitySystemComponent asc)
        {
            if (activeEnemies.TryGetValue(instanceID, out var enemy))
            {
                asc = enemy.ASC;
                return asc != null;
            }
            asc = null;
            return false;
        }

        // ── Wave API (called by IWaveManager) ────────────────────────────────────

        /// <summary>
        /// Enqueue all spawn entries from <paramref name="config"/>, mapped to their
        /// corresponding <paramref name="paths"/> by <see cref="SpawnEntry.pathIndex"/>.
        /// Enemies spawn over time; the delay between each is defined per <see cref="SpawnEntry"/>.
        /// </summary>
        public void BeginWave(WaveConfig config, IReadOnlyList<Vector3>[] paths)
        {
            if (config == null || config.spawnEntries == null) return;

            spawnQueue.Clear();
            isSpawning = true;

            float accumulated = config.startDelay;

            foreach (var entry in config.spawnEntries)
            {
                if (entry == null) continue;

                int pathIdx   = Mathf.Clamp(entry.pathIndex, 0, paths.Length - 1);
                var waypoints = paths[pathIdx];

                for (int i = 0; i < entry.count; i++)
                {
                    spawnQueue.Add(new SpawnQueueEntry
                    {
                        enemyID        = entry.enemyID,
                        maxHealth      = 100f,     // TODO: read from EnemyDataTable
                        moveSpeed      = 3f,       // TODO: read from EnemyDataTable
                        waypoints      = waypoints,
                        delayRemaining = accumulated
                    });

                    accumulated += entry.intervalBetweenSpawns;
                }
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private void ProcessSpawnQueue(float dt)
        {
            if (!isSpawning || spawnQueue.Count == 0)
            {
                isSpawning = false;
                return;
            }

            // Entries are ordered by delayRemaining (ascending).
            // Decrement only the front entry each frame; once it reaches <= 0 spawn it
            // and move to the next. Multiple entries can elapse within one frame.
            while (spawnQueue.Count > 0)
            {
                // Modify the struct in-place via index
                var entry = spawnQueue[0];
                entry.delayRemaining -= dt;

                if (entry.delayRemaining > 0f)
                {
                    spawnQueue[0] = entry;   // write updated delay back
                    break;                   // front not ready yet – done for this frame
                }

                // This entry is ready: consume it, carry any leftover dt into the next entry
                dt = -entry.delayRemaining;  // leftover time after this spawn
                spawnQueue.RemoveAt(0);
                SpawnEnemyInternal(entry.enemyID, entry.maxHealth, entry.moveSpeed, entry.waypoints);
            }

            if (spawnQueue.Count == 0) isSpawning = false;
        }

        private void TickActiveEnemies(float dt)
        {
            foreach (var enemy in activeEnemies.Values)
            {
                if (!enemy.IsAlive || enemy.HasReachedEnd) continue;
                enemy.Tick(dt);
            }
        }

        private void FlushPendingRemovals()
        {
            if (pendingRemoval.Count == 0) return;

            foreach (var enemy in pendingRemoval)
            {
                bool reachedBase = enemy.HasReachedEnd;

                enemy.OnDeath      -= HandleEnemyDeath;
                enemy.OnReachedEnd -= HandleEnemyReachedEnd;
                enemy.Cleanup();                              // removes render layer
                activeEnemies.Remove(enemy.InstanceID);

                OnEnemyRemoved?.Invoke(enemy.InstanceID, reachedBase);
            }

            pendingRemoval.Clear();
        }

        /// <summary>
        /// Core factory: resolves a new Transient <see cref="AbilitySystemComponent"/>
        /// from VContainer, wires an <see cref="Enemy"/>, then registers it.
        /// </summary>
        private int SpawnEnemyInternal(
            string                 enemyID,
            float                  maxHealth,
            float                  moveSpeed,
            IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                Debug.LogWarning($"[EnemyManager] SpawnEnemy '{enemyID}' — waypoints list is empty.");
                return -1;
            }

            // VContainer injects all dependencies into the new ASC instance automatically
            var asc   = container.Resolve<AbilitySystemComponent>();
            var enemy = new Enemy(asc);
            int id    = instanceIDService.GetNextID();

            enemy.Initialize(id, enemyID, maxHealth, moveSpeed, waypoints, renderService);

            enemy.OnDeath      += HandleEnemyDeath;
            enemy.OnReachedEnd += HandleEnemyReachedEnd;

            activeEnemies.Add(id, enemy);
            OnEnemySpawned?.Invoke(id);

            return id;
        }

        private void KillEnemy(Enemy enemy, bool reachedBase, bool notify)
        {
            enemy.OnDeath      -= HandleEnemyDeath;
            enemy.OnReachedEnd -= HandleEnemyReachedEnd;
            enemy.Cleanup();
            activeEnemies.Remove(enemy.InstanceID);

            if (notify)
            {
                OnEnemyRemoved?.Invoke(enemy.InstanceID, reachedBase);
            }
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleEnemyDeath(Enemy enemy)
        {
            // Queue for safe removal – we may be inside the TickActiveEnemies loop
            if (!pendingRemoval.Contains(enemy))
                pendingRemoval.Add(enemy);
        }

        private void HandleEnemyReachedEnd(Enemy enemy)
        {
            if (!pendingRemoval.Contains(enemy))
                pendingRemoval.Add(enemy);
        }
    }
}
