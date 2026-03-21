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

        [Header("VFX Options (Effekseer)")]
        [Tooltip("Thay vì dùng Render2D, dùng VFX (chuỗi map với VFXConfigSO) làm đường đạn bay.")]
        public string trailVfxID = "";

        [Tooltip("ID của hiệu ứng Vụ Nổ / Chạm mục tiêu.")]
        public string hitVfxID = "";

        [Tooltip("Maximum range to search for an enemy.")]
        public float attackRange = 5f;

        [Tooltip("Speed of the bullet.")]
        public float bulletSpeed = 10f;

        [Tooltip("Effect applied to target on hit (optional).")]
        public GameplayEffect hitEffect;

        [Tooltip("Base damage if hitEffect is null.")]
        public float baseDamage = 10f;
    }
}
