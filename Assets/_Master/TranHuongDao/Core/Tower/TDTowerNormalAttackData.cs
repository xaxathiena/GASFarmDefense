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
                     menuName  = "Abel/TranHuongDao/Tower Normal Attack")]
    public class TDTowerNormalAttackData : GameplayAbilityData
    {
        [Header("Attack Configuration")]
        [Tooltip("Radius around the tower that is checked for enemies each tick.")]
        public float attackRange = 8f;

        [Tooltip("Flat damage applied when the bullet hits. Used as fallback if damageEffect is null.")]
        public float damageAmount = 20f;

        [Tooltip("GameplayEffect that carries the damage. If null, damage is applied directly.")]
        public GameplayEffect damageEffect;

        [Header("Bullet Configuration")]
        [Tooltip("Travel speed of the spawned bullet in world-units per second.")]
        public float bulletSpeed = 12f;

        [Tooltip("Distance at which the bullet is considered to have hit its target.")]
        public float collisionThreshold = 0.35f;
    }
}
