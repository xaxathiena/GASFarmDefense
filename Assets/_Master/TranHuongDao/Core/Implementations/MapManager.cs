using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Stub MapManager — returns a single hardcoded path for prototyping.
    /// Replace with data-driven paths (from Tilemap or ScriptableObject) when ready.
    /// </summary>
    public class MapManager : IMapManager, IInitializable
    {
        private IReadOnlyList<Vector3>[] _paths;

        public void Initialize()
        {
            // Hardcoded single lane for prototyping
            _paths = new IReadOnlyList<Vector3>[]
            {
                new List<Vector3>
                {
                    new Vector3(-12f, 0f, 0f),
                    new Vector3(-6f,  0f, 0f),
                    new Vector3(0f,   0f, 0f),
                    new Vector3(6f,   0f, 0f),
                    new Vector3(12f,  0f, 0f),
                }
            };

            Debug.Log("[MapManager] Initialized with 1 hardcoded path.");
        }

        public IReadOnlyList<Vector3>[] GetPaths() => _paths;
    }
}
