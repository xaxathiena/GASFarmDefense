using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    [CreateAssetMenu(fileName = "TD_InstantAoEAbilityData", menuName = "GAS/Abilities/Instant AoE")]
    public class TD_InstantAoEAbilityData : GameplayAbilityData
    {
        [Header("AoE Settings")]
        [Tooltip("What type of targets should this effect apply to?")]
        public EAuraTargetType targetType = EAuraTargetType.Enemies;

        [Tooltip("The radius of the explosion/AoE.")]
        public float radius = 300f;

        [Tooltip("Maximum number of targets to affect. If -1, all targets in range are affected.")]
        public int maxTargets = -1;

        [Tooltip("GameplayEffect to apply for damage. MUST use a SetByCaller modifier with the tag below.")]
        public GameplayEffect damageEffect;
        [Header("Status Effects")]
        [Tooltip("Additional GameplayEffect to apply (e.g., Slow, Stun). Optional.")]
        public GameplayEffect statusEffect;

        [Header("VFX")]
        [Tooltip("Visual effect to spawn at the explosion center.")]
        public string explosionVfxID;
    }
}
