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
                // Find nearest valid target dynamically from the collision point.

                // Track IDs we have already hit or targeted, to avoid bouncing to the same enemy.
                HashSet<int> hitTargets = new HashSet<int>();
                int targetsHit = 0;

                System.Action<Vector3> spawnNextChainedBullet = null;
                spawnNextChainedBullet = (currentPos) =>
                {
                    if (targetsHit >= multiData.maxTargets) return;
                    if (asc == null || asc.GetOwner() == null) return; // safety against tower removal

                    // We allocate a local list for the callback because multiple chains 
                    // could be running concurrently, making a shared class-level list unsafe.
                    List<int> localCache = new List<int>(16);
                    _enemyManager.GetEnemiesInRange(currentPos, multiData.attackRange, localCache);

                    int nextTargetID = -1;
                    float minSqrDist = float.MaxValue;

                    foreach (int id in localCache)
                    {
                        if (hitTargets.Contains(id)) continue;
                        if (_enemyManager.TryGetEnemyPosition(id, out Vector3 enemyPos))
                        {
                            float sqrDist = (enemyPos - currentPos).sqrMagnitude;
                            if (sqrDist < minSqrDist)
                            {
                                minSqrDist = sqrDist;
                                nextTargetID = id;
                            }
                        }
                    }

                    if (nextTargetID == -1) return; // Discontinue chain if no valid targets left

                    hitTargets.Add(nextTargetID);
                    targetsHit++;

                    _bulletManager.SpawnBullet(
                        trailID: multiData.trailID,
                        trailVfxID: multiData.trailVfxID,
                        hitVfxID: multiData.hitVfxID,
                        targetEnemyInstanceID: nextTargetID,
                        spawnPosition: currentPos,
                        sourceASC: asc,
                        damageEffect: multiData.hitEffect,
                        damageAmount: multiData.baseDamage,
                        bulletSpeed: multiData.bulletSpeed,
                        collisionThreshold: 0.5f,
                        sourceAbility: multiData,
                        onHit: (hitPos, _) =>
                        {
                            spawnNextChainedBullet(hitPos);
                        }
                    );
                };

                // Start the chain from the tower's origin position
                spawnNextChainedBullet(originPos);
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
                        collisionThreshold: 0.5f,
                        sourceAbility: multiData
                    );

                    targetsFired++;
                }
            }
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    }
}
