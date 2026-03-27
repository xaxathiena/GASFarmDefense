using GAS;
using UnityEngine;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Configuration for Aura type abilities.
    /// Should be set as ManualEnd so it stays active.
    /// </summary>
    [CreateAssetMenu(menuName = "GAS/Abilities/Aura Data", fileName = "TD_AuraData")]
    public class TD_AuraData : GameplayAbilityData
    {
        [Header("Aura Configuration")]
        [Tooltip("Radius of the aura area.")]
        public float radius = 5f;

        [Tooltip("How often to check for entering/exiting enemies (seconds), then apply effect.")]
        public float tickInterval = 0.5f;

        [Tooltip("Gameplay effect to apply when entering, removed upon exiting.")]
        public GameplayEffect auraEffect;

        [Header("VFX & Duration")]
        [Tooltip("ID of the VFX to play (configured in VFXConfigSO).")]
        public string vfxID;

        [Tooltip("Interval between periodic VFX spawns. If <= 0, spawns once at activation.")]
        public float vfxInterval = 0f;

        [Tooltip("Lifetime of each periodic VFX (only if vfxInterval > 0).")]
        public float vfxLifeTime = 1f;

        [Tooltip("Total duration of the aura. If <= 0, it is infinite.")]
        public float auraDuration = 0f;
    }
}
