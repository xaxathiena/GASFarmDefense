using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// ScriptableObject that configures a tower's basic attack.
    /// Create one in the Project window via Abel → Tower Normal Attack.
    ///
    /// The behaviour class <see cref="TowerAttackAbilityBehaviour"/> resolves at runtime
    /// by name — register that class in <see cref="GameLifetimeScope"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "TD_TowerNormalAttack",
                     menuName = "Abel/TranHuongDao/Tower Normal Attack")]
    public class TDTowerNormalAttackData : GameplayAbilityData
    {
        // attackRange, damageAmount, and bulletSpeed have been removed.
        // Those values are now read at runtime from the owner's UnitAttributeSet
        // (AttackRange, Damage, ProjectileSpeed) so they respond to buffs/debuffs.

        [Header("Damage Mechanic")]
        [Tooltip("GameplayEffect that carries the damage. If null, damage is applied directly.")]
        public GameplayEffect damageEffect;

        [Header("Bullet Mechanic")]
        [Tooltip("The ID of the visual trail/projectile to use.")]
        public string trailID = "bullet_normal";

        [Tooltip("Distance at which the bullet is considered to have hit its target.")]
        public float collisionThreshold = 0.35f;

        // cooldownDuration (inherited from GameplayAbilityData) must be set to 0
        // in the ScriptableObject. The behaviour overrides it dynamically via
        // asc.StartCooldown using UnitAttributeSet.AttackCooldown.CurrentValue.
    }
}
