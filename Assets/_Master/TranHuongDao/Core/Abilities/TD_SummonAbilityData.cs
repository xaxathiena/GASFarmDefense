using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Data for an ability that summons a minion.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Abilities/Summon Ability Data", fileName = "TD_SummonAbilityData")]
    public class TD_SummonAbilityData : GameplayAbilityData
    {
        [Header("Summon Configuration")]
        [Tooltip("The ID of the unit to summon (from UnitsConfig).")]
        public string unitID;

        [Tooltip("Optional logic override. If None, uses the unit's default LogicType.")]
        public EUnitLogicType overrideLogic = EUnitLogicType.None;

        [Tooltip("Offset relative to the owner to spawn the minion.")]
        public Vector3 spawnOffset = Vector3.forward;

        [Tooltip("How many to spawn at once.")]
        public int count = 1;
    }
}
