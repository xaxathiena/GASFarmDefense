using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Manages the lifecycle of all active bullets in the scene.
    /// Bullets are pure C# objects — no Physics, no MonoBehaviour.
    /// </summary>
    public interface IBulletManager
    {
        /// <summary>
        /// Spawn a new bullet that will travel from <paramref name="spawnPosition"/>
        /// toward the enemy identified by <paramref name="targetEnemyInstanceID"/>.
        ///
        /// When the bullet reaches the enemy it applies <paramref name="damageEffect"/>
        /// (or direct <paramref name="damageAmount"/> if effect is null) to the enemy's ASC.
        /// </summary>
        /// <param name="targetEnemyInstanceID">InstanceID of the target enemy.</param>
        /// <param name="spawnPosition">World-space spawn point (usually the tower's position).</param>
        /// <param name="sourceASC">ASC of the tower — used as the GE source.</param>
        /// <param name="damageEffect">GameplayEffect to apply on hit (may be null).</param>
        /// <param name="damageAmount">Fallback flat damage when <paramref name="damageEffect"/> is null.</param>
        /// <param name="bulletSpeed">Travel speed in units per second.</param>
        /// <param name="collisionThreshold">Distance threshold that counts as a hit.</param>
        void SpawnBullet(
            int              targetEnemyInstanceID,
            Vector3          spawnPosition,
            AbilitySystemComponent sourceASC,
            GAS.GameplayEffect damageEffect,
            float            damageAmount,
            float            bulletSpeed,
            float            collisionThreshold);
    }
}
