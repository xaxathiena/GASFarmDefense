using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// ScriptableObject data for a tower's active skill.
    /// The skill applies <see cref="skillEffect"/> to enemies found by the behaviour.
    ///
    /// AoE mode (aoeRadius > 0): hits every alive enemy within aoeRadius of the tower.
    /// Single-target mode (aoeRadius == 0): hits only the closest enemy within attack range.
    ///
    /// cooldownDuration (inherited from GameplayAbilityData) controls how often the skill fires.
    /// </summary>
    [CreateAssetMenu(fileName = "TD_TowerSkill",
                     menuName  = "Abel/TranHuongDao/Tower Skill")]
    public class TDTowerSkillData : GameplayAbilityData
    {
        [Header("Skill Effect")]
        [Tooltip("GameplayEffect applied to each enemy that is hit.")]
        public GameplayEffect skillEffect;

        [Header("AoE")]
        [Tooltip("Radius around the tower to collect targets. 0 = single closest target within AttackRange.")]
        public float aoeRadius = 0f;
    }
}
