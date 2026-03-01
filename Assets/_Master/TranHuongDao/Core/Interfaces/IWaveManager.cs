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


}
