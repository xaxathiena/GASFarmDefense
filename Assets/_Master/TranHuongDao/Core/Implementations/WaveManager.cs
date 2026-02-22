using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Controls wave progression: queues waves, delegates spawning to IEnemyManager,
    /// and tracks when a wave is fully cleared.
    /// </summary>
    public class WaveManager : IWaveManager, IInitializable, IStartable
    {
        // ── State ─────────────────────────────────────────────────────────────────
        public int CurrentWaveIndex { get; private set; }
        public int TotalWaves { get; private set; }
        public bool IsWaveRunning { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnAllWavesCompleted;

        // ── Dependencies ──────────────────────────────────────────────────────────
        private readonly IEnemyManager _enemyManager;
        private readonly IMapManager _mapManager;

        // ── Internal ──────────────────────────────────────────────────────────────
        private IReadOnlyList<WaveConfig> _waveConfigs;
        private int _enemiesRemaining;

        // ── Constructor ───────────────────────────────────────────────────────────
        public WaveManager(IEnemyManager enemyManager, IMapManager mapManager)
        {
            _enemyManager = enemyManager;
            _mapManager = mapManager;
            _enemyManager.OnEnemyRemoved += NotifyEnemyDefeated;
        }

        private void NotifyEnemyDefeated(int arg1, bool arg2)
        {
            NotifyEnemyDefeated();
        }

        // ── IInitializable ────────────────────────────────────────────────────────
        /// <summary>
        /// Called by VContainer after all dependencies are injected.
        /// Bootstraps a hardcoded demo wave. Replace with data-driven configs when ready.
        /// </summary>
        public void Initialize()
        {
            var demoWaves = new List<WaveConfig>
            {
                new WaveConfig
                {
                    waveName     = "Wave 1",
                    startDelay   = 2f,
                    spawnEntries = new List<SpawnEntry>
                    {
                        new SpawnEntry { enemyID = "unit_champion_boar", count = 5,  intervalBetweenSpawns = 1.0f, pathIndex = 0 },
                    }
                },
                new WaveConfig
                {
                    waveName     = "Wave 2",
                    startDelay   = 1f,
                    spawnEntries = new List<SpawnEntry>
                    {
                        new SpawnEntry { enemyID = "unit_champion_boar", count = 8,  intervalBetweenSpawns = 0.8f, pathIndex = 0 },
                    }
                },
                new WaveConfig
                {
                    waveName     = "Wave 3 — Boss",
                    startDelay   = 1f,
                    spawnEntries = new List<SpawnEntry>
                    {
                        new SpawnEntry { enemyID = "unit_champion_boar", count = 10, intervalBetweenSpawns = 0.6f, pathIndex = 0 },
                    }
                },
            };

            Initialize(demoWaves);
            Debug.Log($"[WaveManager] Initialized with {TotalWaves} demo waves.");
        }

        // ── IWaveManager ──────────────────────────────────────────────────────────

        public void Initialize(IReadOnlyList<WaveConfig> waveConfigs)
        {
            _waveConfigs = waveConfigs;
            TotalWaves = waveConfigs.Count;
            CurrentWaveIndex = 0;
            IsWaveRunning = false;
            _enemiesRemaining = 0;
        }
        public void Start()
        {
            StartNextWave();
        }
        public void StartNextWave()
        {
            if (IsWaveRunning)
            {
                Debug.LogWarning("[WaveManager] StartNextWave called while a wave is already running.");
                return;
            }

            if (CurrentWaveIndex >= TotalWaves)
            {
                Debug.LogWarning("[WaveManager] No more waves to start.");
                return;
            }

            var config = _waveConfigs[CurrentWaveIndex];

            // Count total enemies for this wave
            _enemiesRemaining = 0;
            if (config.spawnEntries != null)
                foreach (var entry in config.spawnEntries)
                    if (entry != null) _enemiesRemaining += entry.count;

            // Delegate spawning to EnemyManager
            var paths = _mapManager.GetPaths();
            _enemyManager.BeginWave(config, paths);

            IsWaveRunning = true;
            int startedIndex = CurrentWaveIndex;
            CurrentWaveIndex++;

            OnWaveStarted?.Invoke(startedIndex);
            Debug.Log($"[WaveManager] Wave {startedIndex} started — {_enemiesRemaining} enemies.");
        }

        public void NotifyEnemyDefeated()
        {
            if (!IsWaveRunning) return;

            _enemiesRemaining--;

            if (_enemiesRemaining > 0) return;

            // Wave cleared
            int completedIndex = CurrentWaveIndex - 1;
            IsWaveRunning = false;
            OnWaveCompleted?.Invoke(completedIndex);
            Debug.Log($"[WaveManager] Wave {completedIndex} completed.");

            if (CurrentWaveIndex >= TotalWaves)
            {
                OnAllWavesCompleted?.Invoke();
                Debug.Log("[WaveManager] All waves completed!");
            }
        }
    }
}
