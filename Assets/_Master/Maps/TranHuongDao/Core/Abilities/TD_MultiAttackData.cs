using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Data container for Multi-target attacks (Simultaneous & Sequential/Chain).
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Abilities/Multi Attack Data", fileName = "TD_MultiAttackData")]
    public class TD_MultiAttackData : GameplayAbilityData
    {
        [Header("Weapon Config")]
        public string trailID = "bullet_chain";
        [Header("VFX Options (Effekseer)")]
        public string trailVfxID = "";
        public string hitVfxID = "";
        public float attackRange = 5f;
        public float bulletSpeed = 10f;
        public GameplayEffect hitEffect;
        public float baseDamage = 15f;

        [Header("Multi-Target Settings")]
        [Tooltip("If true, fires chaining projectiles. If false, fires simultaneously to multiple targets.")]
        public bool isSequential = false;

        [Tooltip("Max dynamic targets (or bounces if sequential).")]
        public int maxTargets = 3;

        [Tooltip("Max times the same target can be hit (used in sequential mode).")]
        public int maxHitsPerTarget = 1;

        [Tooltip("Bounce range if sequential mode is enabled.")]
        public float searchRadius = 4f;
    }
}
