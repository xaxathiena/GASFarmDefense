using System;
using System.Collections.Generic;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Configuration binding waves to specific maps.
    /// Manages per-map wave progressions.
    /// </summary>
    [Serializable]
    public struct MapWaveProfile
    {
        /// <summary>Unique identifier for the map.</summary>
        public string MapID;

        /// <summary>List of wave configurations sequentially running in this map.</summary>
        public List<WaveConfig> waves;
    }

    /// <summary>
    /// Global ScriptableObject database handling waves configuration for multiple maps.
    /// Derives from BaseConfigSO to natively integrate with ConfigService.
    /// </summary>
    [CreateAssetMenu(fileName = "WavesConfig", menuName = "Abel/TranHuongDao/WavesConfig")]
    public class WavesConfig : BaseConfigSO
    {
        /// <summary>List defining waves per map.</summary>
        [SerializeField]
        public List<MapWaveProfile> mapProfiles;

        // Internal lookup cache for O(1) fetching at runtime.
        private Dictionary<string, IReadOnlyList<WaveConfig>> _wavesByMap;

        /// <summary>
        /// Reads through the inspector-provided configuration list and constructs
        /// an optimized runtime dictionary mapped by MapID.
        /// </summary>
        public override void InitializeConfig()
        {
            _wavesByMap = new Dictionary<string, IReadOnlyList<WaveConfig>>(StringComparer.OrdinalIgnoreCase);

            if (mapProfiles == null) return;

            foreach (var profile in mapProfiles)
            {
                if (string.IsNullOrWhiteSpace(profile.MapID)) continue;

                if (_wavesByMap.ContainsKey(profile.MapID))
                {
                    Debug.LogWarning($"[WavesConfig] Duplicate MapID found: {profile.MapID}. Overriding previous definition.");
                }

                _wavesByMap[profile.MapID] = profile.waves;
            }
        }

        /// <summary>
        /// Attempts to get the read-only list of wave configurations configured for the specified map.
        /// </summary>
        /// <param name="mapID">Identifier string matching the MapID defined in profiles.</param>
        /// <param name="waves">The fetched read-only wave config list, if any.</param>
        /// <returns>True if the map waves exist and were successfully returned.</returns>
        public bool TryGetWavesForMap(string mapID, out IReadOnlyList<WaveConfig> waves)
        {
            // Ensure dictionary is constructed just in case InitializeConfig wasn't explicitly called prior
            if (_wavesByMap == null)
            {
                InitializeConfig();
            }

            return _wavesByMap.TryGetValue(mapID, out waves);
        }
    }
}
