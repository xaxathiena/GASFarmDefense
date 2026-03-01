using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Abel.TowerDefense.Config;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Controls wave progression: queues waves, delegates spawning to IEnemyManager,
    /// tracks when a wave is fully cleared, and handles preparation delays between waves.
    /// </summary>
    public class WaveManager : IWaveManager, IInitializable, IStartable, ITickable
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
        private readonly IMapLayoutManager _mapManager;
        private readonly IConfigService _configService;

        // ── Internal ──────────────────────────────────────────────────────────────
        private IReadOnlyList<WaveConfig> _waveConfigs;
        private int _enemiesRemaining;
        private float _currentWaitTimer;
        private bool _isWaitingForNextWave;

        // ── Constructor ───────────────────────────────────────────────────────────
        public WaveManager(IEnemyManager enemyManager, IMapLayoutManager mapManager, IConfigService configService)
        {
            _enemyManager = enemyManager;
            _mapManager = mapManager;
            _configService = configService;

            _enemyManager.OnEnemyRemoved += NotifyEnemyDefeated;
        }

        private void NotifyEnemyDefeated(int arg1, bool arg2)
        {
            NotifyEnemyDefeated();
        }

        // ── IInitializable ────────────────────────────────────────────────────────
        /// <summary>
        /// Called by VContainer after all dependencies are injected.
        /// Loads wave data from WavesConfig for the current map.
        /// </summary>
        public void Initialize()
        {
            var wavesConfig = _configService.GetConfig<WavesConfig>();
            if (wavesConfig == null)
            {
                Debug.LogError("[WaveManager] Fatal: WavesConfig not found in ConfigService.");
                return;
            }

            // Future proofing for reading the MapID from IMapLayoutManager
            // For now, hardcode to Map_1 as it serves as the default map testing phase
            string mapId = "Map_1";

            if (!wavesConfig.TryGetWavesForMap(mapId, out IReadOnlyList<WaveConfig> mapWaves))
            {
                Debug.LogError($"[WaveManager] No wave configuration found for MapID: {mapId}");
                return;
            }

            Initialize(mapWaves);
            Debug.Log($"[WaveManager] Initialized with {TotalWaves} waves for map {mapId}.");
        }

        // ── IWaveManager ──────────────────────────────────────────────────────────

        public void Initialize(IReadOnlyList<WaveConfig> waveConfigs)
        {
            _waveConfigs = waveConfigs;
            TotalWaves = waveConfigs?.Count ?? 0;
            CurrentWaveIndex = 0;
            IsWaveRunning = false;
            _enemiesRemaining = 0;
            _isWaitingForNextWave = false;
            _currentWaitTimer = 0f;
        }

        public void Start()
        {
            if (TotalWaves > 0)
            {
                PrepareForNextWave();
            }
        }

        /// <summary>
        /// Handles the countdown timer between waves.
        /// </summary>
        public void Tick()
        {
            if (!_isWaitingForNextWave) return;

            _currentWaitTimer -= Time.deltaTime;

            if (_currentWaitTimer <= 0f)
            {
                _isWaitingForNextWave = false;
                StartNextWave();
            }
        }

        /// <summary>
        /// Starts the timer for the upcoming wave based on its preparationTime.
        /// </summary>
        private void PrepareForNextWave()
        {
            if (CurrentWaveIndex >= TotalWaves) return;

            WaveConfig nextWave = _waveConfigs[CurrentWaveIndex];
            _currentWaitTimer = nextWave.preparationTime;
            _isWaitingForNextWave = true;
            Debug.Log($"[WaveManager] Waiting {_currentWaitTimer}s before starting wave {CurrentWaveIndex} ({nextWave.waveName}).");
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

            _isWaitingForNextWave = false; // Ensure we are no longer waiting just in case manually triggered

            var config = _waveConfigs[CurrentWaveIndex];

            // Count total enemies for this wave
            _enemiesRemaining = 0;
            if (config.spawnEntries != null)
            {
                foreach (var entry in config.spawnEntries)
                {
                    if (entry != null) _enemiesRemaining += entry.count;
                }
            }

            // Delegate spawning to EnemyManager
            var paths = _mapManager.GetEnemyPath();
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
            else
            {
                PrepareForNextWave();
            }
        }
    }
}
