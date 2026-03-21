using System;
using System.Collections.Generic;
using UnityEngine;
using Abel.TowerDefense.Config; // Assuming BaseConfigSO is here based on previous context or Abel.TranHuongDao.Core if local. Wait, let me check where BaseConfigSO is.

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Represents a single continuous path composed of multiple waypoints.
    /// </summary>
    [Serializable]
    public class PathData
    {
        public List<Vector3> waypoints = new List<Vector3>();
    }

    /// <summary>
    /// Configuration for a specific map layout, including grid dimensions,
    /// obstacles, and multiple viable paths for enemies to traverse.
    /// </summary>
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Abel/TranHuongDao/MapConfig")]
    public class MapConfigSO : BaseConfigSO
    {
        [Header("Map Identification")]
        public string MapID;

        [Header("Grid Definition")]
        public int GridWidth = 20;
        public int GridHeight = 10;
        public float CellSize = 1f;
        public Vector3 OriginPosition = Vector3.zero;

        [Header("Layout Data")]
        /// <summary>
        /// A collection of predefined paths that enemies can take across this map.
        /// </summary>
        public List<PathData> EnemyPaths = new List<PathData>();

        /// <summary>
        /// Whitelist of grid cells where towers are allowed to be placed.
        /// Any cell NOT in this list is considered non-buildable.
        /// </summary>
        public List<Vector2Int> BuildableCells = new List<Vector2Int>();

        public override void InitializeConfig()
        {
            // Initialization logic if required by BaseConfigSO
        }
    }
}
