using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Configuration for Aura type abilities.
    /// Should be set as ManualEnd so it stays active.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Abilities/Aura Data", fileName = "TD_AuraData")]
    public class TD_AuraData : GameplayAbilityData
    {
        [Header("Aura Configuration")]
        [Tooltip("Radius of the aura area.")]
        public float radius = 5f;
        
        [Tooltip("How often to check for entering/exiting enemies (seconds).")]
        public float tickInterval = 0.5f;
        
        [Tooltip("Gameplay effect to apply when entering, removed upon exiting.")]
        public GameplayEffect auraEffect;
    }
}
