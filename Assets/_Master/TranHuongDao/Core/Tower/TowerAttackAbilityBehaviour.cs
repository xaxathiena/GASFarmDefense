using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// IAbilityBehaviour implementation for <see cref="TDTowerNormalAttackData"/>.
    /// Registered as a Singleton in VContainer and mapped explicitly by
    /// <see cref="TDGASInitializer"/>.
    ///
    /// Flow each time the tower calls TryActivateAbility:
    ///   CanActivate → verify at least one enemy is within range
    ///   OnActivated → spawn a <see cref="Bullet"/> via <see cref="IBulletManager"/>
    ///
    /// The bullet travels toward the target and applies damage on impact.
    /// </summary>
    public class TDTowerNormalAttackBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager  _enemyManager;
        private readonly IBulletManager _bulletManager;

        public TDTowerNormalAttackBehaviour(
            IEnemyManager  enemyManager,
            IBulletManager bulletManager)
        {
            _enemyManager  = enemyManager;
            _bulletManager = bulletManager;
        }

        // ── IAbilityBehaviour ────────────────────────────────────────────────────

        public bool CanActivate(
            GameplayAbilityData data,
            AbilitySystemComponent asc,
            GameplayAbilitySpec spec)
        {
            var attackData = data as TDTowerNormalAttackData;
            if (attackData == null) return false;

            Vector3 origin = GetOwnerPosition(asc);
            int targetID   = _enemyManager.GetClosestEnemyInRange(origin, attackData.attackRange);
            return targetID != -1;
        }

        public void OnActivated(
            GameplayAbilityData data,
            AbilitySystemComponent asc,
            GameplayAbilitySpec spec)
        {
            var attackData = data as TDTowerNormalAttackData;
            if (attackData == null) return;

            Vector3 spawnPos = GetOwnerPosition(asc);
            int targetID     = _enemyManager.GetClosestEnemyInRange(spawnPos, attackData.attackRange);
            if (targetID == -1) return;

            // ── Spawn a bullet that will travel to the target ─────────────────────
            _bulletManager.SpawnBullet(
                targetEnemyInstanceID : targetID,
                spawnPosition         : spawnPos,
                sourceASC             : asc,
                damageEffect          : attackData.damageEffect,
                damageAmount          : attackData.damageAmount,
                bulletSpeed           : attackData.bulletSpeed,
                collisionThreshold    : attackData.collisionThreshold);

            Debug.Log($"[TowerAttack] Bullet fired at enemy {targetID}");
        }

        public void OnEnded    (GameplayAbilityData _, AbilitySystemComponent __, GameplayAbilitySpec ___) { }
        public void OnCancelled(GameplayAbilityData _, AbilitySystemComponent __, GameplayAbilitySpec ___) { }

        // ── Private ──────────────────────────────────────────────────────────────

        private static Vector3 GetOwnerPosition(AbilitySystemComponent asc)
        {
            var owner = asc.GetOwner();
            return owner != null ? owner.position : Vector3.zero;
        }
    }
}
