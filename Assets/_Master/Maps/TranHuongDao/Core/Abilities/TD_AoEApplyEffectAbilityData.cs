using System.Collections.Generic;
using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
using UnityEngine;

namespace Abel.TranHuongDao.Core.Abilities
{
    public enum EAuraTargetType
    {
        Enemies,
        Allies,
        Both
    }

    [CreateAssetMenu(fileName = "TD_AoESlowAbilityData", menuName = "GAS/Abilities/AoE Apply Effect")]
    public class TD_AoEApplyEffectAbilityData : GameplayAbilityData
    {
        [Header("AoE Apply Effect Settings")]
        [Tooltip("What type of targets should this effect apply to?")]
        public EAuraTargetType targetType = EAuraTargetType.Enemies;

        [Tooltip("The radius around the caster to find targets.")]
        public float captureRadius = 800f;

        [Tooltip("How often (in seconds) the area is swept to apply/remove the effect.")]
        public float auraUpdateRate = 0.5f;

        [Tooltip("The Gameplay Effects to apply (e.g., Slow, Defense Reduce, Attack Reduce, etc.).")]
        public List<GameplayEffect> effectsToApply;

        [Header("VFX")]
        [Tooltip("Visual effect to instantiate at the caster position when activated.")]
        public GameObject castVfxPrefab;
    }
}


