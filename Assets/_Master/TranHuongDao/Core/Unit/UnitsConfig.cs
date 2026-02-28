using UnityEngine;
using System.Collections.Generic;

namespace Abel.TranHuongDao.Core
{
    [CreateAssetMenu(fileName = "UnitsConfig", menuName = "Abel/TranHuongDao/Units Config Database")]
    public class UnitsConfig : BaseConfigSO
    {
        [Header("Runtime Database")]
        public List<UnitConfigData> unitEntries = new List<UnitConfigData>();

        // Built once by InitializeConfig(); provides O(1) lookup at runtime.
        private Dictionary<string, UnitConfigData> _lookup;

        /// <summary>
        /// Called once at startup by ConfigService. Builds the fast lookup dictionary.
        /// </summary>
        public override void InitializeConfig()
        {
            _lookup = new Dictionary<string, UnitConfigData>(unitEntries.Count, System.StringComparer.Ordinal);
            foreach (var entry in unitEntries)
            {
                if (!string.IsNullOrEmpty(entry.UnitID))
                    _lookup[entry.UnitID] = entry;
            }
            Debug.Log($"[UnitsConfig] Indexed {_lookup.Count} unit configs.");
        }

        /// <summary>O(1) lookup after InitializeConfig() has run.</summary>
        public bool TryGetConfig(string id, out UnitConfigData configData)
        {
            if (_lookup != null)
                return _lookup.TryGetValue(id, out configData);

            // Fallback linear search (editor-only, before InitializeConfig runs).
            foreach (var entry in unitEntries)
            {
                if (entry.UnitID == id)
                {
                    configData = entry;
                    return true;
                }
            }
            configData = default;
            return false;
        }
    }
}