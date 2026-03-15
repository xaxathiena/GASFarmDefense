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
            if (attackData == null || asc?.Avatar == null) return false;

            // Only activate if an enemy is in range
            int closestEnemyID = _enemyManager.GetClosestEnemyInRange(asc.Position, attackData.attackRange);
            return closestEnemyID != -1;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var attackData = data as TD_NormalAttackData;
            if (attackData == null) return;

            Vector3 ownerPos = asc.Position;
            int targetEnemyID = _enemyManager.GetClosestEnemyInRange(ownerPos, attackData.attackRange);
            if (targetEnemyID == -1) return;

            // Spawn bullet at owner's position
            _bulletManager.SpawnBullet(
                attackData.trailID,
                attackData.trailVfxID,
                attackData.hitVfxID,
                targetEnemyID,
                ownerPos,
                sourceASC: asc,
                damageEffect: attackData.hitEffect,
                damageAmount: attackData.baseDamage,
                bulletSpeed: attackData.bulletSpeed,
                collisionThreshold: 0.5f,
                sourceAbility: attackData
            );
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { /* InstantEnd cleans up automatically */ }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    }
}
