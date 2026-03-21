using System;
using System.Collections.Generic;

namespace Abel.TranHuongDao.Core
{
    /// <summary>Defines the enemies and timing for a single wave.</summary>
    [Serializable]
    public class WaveConfig
    {
        /// <summary>Human-readable label, e.g. "Wave 3 - Elite Rush".</summary>
        public string waveName;

        /// <summary>Time to wait in seconds BEFORE this wave starts, allowing the player to build towers.</summary>
        public float preparationTime;

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
