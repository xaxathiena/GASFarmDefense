using System.Collections.Generic;
using UnityEngine;
using VContainer;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Implements Multi-Attack logic: Simultaneously or chain-firing bullets.
    /// </summary>
    public class TD_MultiAttackBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        private readonly IBulletManager _bulletManager;

        // Cache to avoid runtime GC alloc during Overlap requests
        private readonly List<int> _enemyCache = new List<int>(16);

        [Inject]
        public TD_MultiAttackBehaviour(IEnemyManager enemyManager, IBulletManager bulletManager)
        {
            _enemyManager = enemyManager;
            _bulletManager = bulletManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var multiData = data as TD_MultiAttackData;
            if (multiData == null || asc?.GetOwner() == null) return false;

            return _enemyManager.GetClosestEnemyInRange(asc.GetOwner().position, multiData.attackRange) != -1;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var multiData = data as TD_MultiAttackData;
            if (multiData == null || asc?.GetOwner() == null) return;

            Vector3 originPos = asc.GetOwner().position;

            _enemyCache.Clear();
            _enemyManager.GetEnemiesInRange(originPos, multiData.attackRange, _enemyCache);

            if (_enemyCache.Count == 0) return;

            if (multiData.isSequential)
            {
                // Sequential (Chain) Mode
                // Typically fires 1 bullet first, when that bullet hits, the BulletManager 
                // bounces it dynamically. We pass bounce limits to the bullet manager if API allowed.
                // Assuming BulletManager handles single target spawn, and relies on an event for bounce logic.
                // We'll spawn the initial projectile for the closest enemy here.

                int closestID = _enemyManager.GetClosestEnemyInRange(originPos, multiData.attackRange);
                if (closestID != -1)
                {
                    _bulletManager.SpawnBullet(
                        trailID: multiData.trailID, // The chained trail ID
                        trailVfxID: multiData.trailVfxID,
                        hitVfxID: multiData.hitVfxID,
                        targetEnemyInstanceID: closestID,
                        spawnPosition: originPos,
                        sourceASC: asc,
                        damageEffect: multiData.hitEffect,
                        damageAmount: multiData.baseDamage,
                        bulletSpeed: 25f,
                        collisionThreshold: 0.5f
                    );
                }
            }
            else
            {
                // Simultaneous Mode
                // Spawn projectile for up to maxTargets enemies directly.
                int targetsFired = 0;
                foreach (int enemyID in _enemyCache)
                {
                    if (targetsFired >= multiData.maxTargets) break;

                    _bulletManager.SpawnBullet(
                        trailID: multiData.trailID,
                        trailVfxID: multiData.trailVfxID,
                        hitVfxID: multiData.hitVfxID,
                        targetEnemyInstanceID: enemyID,
                        spawnPosition: originPos,
                        sourceASC: asc,
                        damageEffect: multiData.hitEffect,
                        damageAmount: multiData.baseDamage,
                        bulletSpeed: 20f,
                        collisionThreshold: 0.5f
                    );

                    targetsFired++;
                }
            }
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    }
}
