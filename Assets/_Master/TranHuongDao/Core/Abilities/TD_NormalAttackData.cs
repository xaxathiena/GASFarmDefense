using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Pure data container for Normal Attack capability.
    /// Inherits from GAS.GameplayAbilityData to support ASC registration.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Abilities/Normal Attack Data", fileName = "TD_NormalAttackData")]
    public class TD_NormalAttackData : GameplayAbilityData
    {
        [Header("Normal Attack Configuration")]
        [Tooltip("The ID of the visual trail/projectile to use. (BulletManager only needs this ID)")]
        public string trailID = "bullet_default";
        
        [Tooltip("Maximum range to search for an enemy.")]
        public float attackRange = 5f;

        [Tooltip("Effect applied to target on hit (optional).")]
        public GameplayEffect hitEffect;
        
        [Tooltip("Base damage if hitEffect is null.")]
        public float baseDamage = 10f;
    }
}
