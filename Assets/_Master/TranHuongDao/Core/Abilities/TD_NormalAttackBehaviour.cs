using UnityEngine;
using VContainer;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Business logic for TD_NormalAttackData.
    /// Resolved via VContainer as a Singleton and mapped to the Data class.
    /// </summary>
    public class TD_NormalAttackBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        private readonly IBulletManager _bulletManager;

        [Inject]
        public TD_NormalAttackBehaviour(IEnemyManager enemyManager, IBulletManager bulletManager)
        {
            _enemyManager = enemyManager;
            _bulletManager = bulletManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var attackData = data as TD_NormalAttackData;
            if (attackData == null || asc?.GetOwner() == null) return false;

            // Only activate if an enemy is in range
            int closestEnemyID = _enemyManager.GetClosestEnemyInRange(asc.GetOwner().position, attackData.attackRange);
            return closestEnemyID != -1;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var attackData = data as TD_NormalAttackData;
            if (attackData == null) return;

            Transform ownerTransform = asc.GetOwner();
            if (ownerTransform == null) return;

            int targetEnemyID = _enemyManager.GetClosestEnemyInRange(ownerTransform.position, attackData.attackRange);
            if (targetEnemyID == -1) return;

            // Clean logical separation: request spawn by trailID.
            _bulletManager.SpawnBullet(
                trailID: attackData.trailID,
                trailVfxID: attackData.trailVfxID,
                hitVfxID: attackData.hitVfxID,
                targetEnemyInstanceID: targetEnemyID,
                spawnPosition: ownerTransform.position,
                sourceASC: asc,
                damageEffect: attackData.hitEffect,
                damageAmount: attackData.baseDamage,
                bulletSpeed: 20f,
                collisionThreshold: 0.5f
            );
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { /* InstantEnd cleans up automatically */ }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    }
}
