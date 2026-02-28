using UnityEngine;
using System;
using System.Collections.Generic;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Central registry that holds every GameplayAbilityData asset in the project.
    /// Assign all ability ScriptableObjects here so they can be looked up at runtime.
    ///
    /// The key used for lookup is the ability ID (ability.abilityID), which matches UnitConfig.AttackAbilityID / SkillAbilityID.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilitiesConfig", menuName = "Abel/TranHuongDao/Abilities Config Database")]
    public class AbilitiesConfig : BaseConfigSO
    {
        [Header("Ability Registry")]
        public List<GameplayAbilityData> allAbilities = new List<GameplayAbilityData>();

        // Built once by InitializeConfig(). Provides O(1) lookup by ability ID.
        private Dictionary<string, GameplayAbilityData> _lookup;

        /// <summary>
        /// Called once at startup by ConfigService. Builds the fast lookup dictionary.
        /// </summary>
        public override void InitializeConfig()
        {
            _lookup = new Dictionary<string, GameplayAbilityData>(allAbilities.Count, StringComparer.Ordinal);
            foreach (var ability in allAbilities)
            {
                if (ability == null) continue;
                if (!_lookup.ContainsKey(ability.abilityID))
                    _lookup[ability.abilityID] = ability;
                else
                    Debug.LogWarning($"[AbilitiesConfig] Duplicate ability ID '{ability.abilityID}' — second entry ignored.");
            }
            Debug.Log($"[AbilitiesConfig] Indexed {_lookup.Count} abilities.");
        }

        /// <summary>
        /// O(1) lookup after InitializeConfig() has run.
        /// Falls back to a linear search in the editor (before ConfigService initialises).
        /// </summary>
        /// <param name="id">The ability ID, e.g. "TD_TowerNormalAttack".</param>
        public bool TryGetAbility(string id, out GameplayAbilityData ability)
        {
            if (_lookup != null)
                return _lookup.TryGetValue(id, out ability);

            // Fallback linear search (editor play-mode entry or pre-init calls)
            foreach (var a in allAbilities)
            {
                if (a != null && a.abilityID == id)
                {
                    ability = a;
                    return true;
                }
            }
            ability = null;
            return false;
        }
    }
}
