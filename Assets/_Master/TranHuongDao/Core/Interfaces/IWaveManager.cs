using System;
using System.Collections.Generic;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Controls wave progression. Reads wave config data and delegates enemy
    /// spawning to IEnemyManager. Does NOT own enemy lifecycle.
    /// </summary>
    public interface IWaveManager
    {
        // --- State ---

        /// <summary>Current wave index (0-based).</summary>
        int CurrentWaveIndex { get; }

        /// <summary>Total number of waves in this stage.</summary>
        int TotalWaves { get; }

        /// <summary>True while a wave is actively running (enemies still spawning or alive).</summary>
        bool IsWaveRunning { get; }

        // --- Events ---

        /// <summary>Fired when a new wave begins. Arg: wave index.</summary>
        event Action<int> OnWaveStarted;

        /// <summary>Fired when all enemies in the current wave are defeated. Arg: wave index.</summary>
        event Action<int> OnWaveCompleted;

        /// <summary>Fired when all waves are finished (stage cleared).</summary>
        event Action OnAllWavesCompleted;

        // --- Commands ---

        /// <summary>Load wave config for the current stage and reset state.</summary>
        void Initialize(IReadOnlyList<WaveConfig> waveConfigs);

        /// <summary>Start the next (or first) wave. No-op if a wave is already running.</summary>
        void StartNextWave();

        /// <summary>
        /// Called by IEnemyManager (or an observer) when an enemy dies.
        /// The wave manager uses this to detect when a wave is fully cleared.
        /// </summary>
        void NotifyEnemyDefeated();
    }

    // ---------------------------------------------------------------------------
    // Data types owned by the wave layer
    // ---------------------------------------------------------------------------

    /// <summary>Defines the enemies and timing for a single wave.</summary>
    [Serializable]
    public class WaveConfig
    {
        /// <summary>Human-readable label, e.g. "Wave 3 – Elite Rush".</summary>
        public string waveName;

        /// <summary>Delay in seconds before the first enemy spawns after wave start.</summary>
        public float startDelay;

        /// <summary>Ordered list of spawn entries in this wave.</summary>
        public List<SpawnEntry> spawnEntries;
    }

    /// <summary>One spawn event inside a wave.</summary>
    [Serializable]
    public class SpawnEntry
    {
        /// <summary>Must match an ID registered in UnitRenderDatabase / EnemyDataTable.</summary>
        public string enemyID;

        /// <summary>How many of this enemy to spawn.</summary>
        public int count;

        /// <summary>Seconds between each individual spawn within this entry.</summary>
        public float intervalBetweenSpawns;

        /// <summary>Index into the path waypoints array for this spawn group.</summary>
        public int pathIndex;
    }
}
