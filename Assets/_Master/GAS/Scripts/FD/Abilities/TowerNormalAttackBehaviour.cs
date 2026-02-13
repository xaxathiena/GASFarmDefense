using UnityEngine;
using GAS;
using System.Collections.Generic;

namespace FD.Abilities
{
    /// <summary>
    /// Behaviour logic for Tower Normal Attack.
    /// Singleton service that handles tower basic attack logic.
    /// Fires at up to 2 enemies every 2 seconds (via cooldown).
    /// </summary>
    public class TowerNormalAttackBehaviour : IAbilityBehaviour
    {
        private readonly IDebugService debug;
        private readonly EnemyManager enemyManager;
        
        public TowerNormalAttackBehaviour(IDebugService debug, EnemyManager enemyManager)
        {
            this.debug = debug;
            this.enemyManager = enemyManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var attackData = data as TowerNormalAttackData;
            if (attackData == null)
            {
                Debug.LogError("Invalid data type for TowerNormalAttackBehaviour");
                return false;
            }

            // Check if there are enemies in range
            var owner = asc.GetData().Owner;
            if (owner == null)
            {
                return false;
            }

            // Get targets from EnemyManager
            var targets = enemyManager.GetEnemiesInRange(owner.position, attackData.attackRange, LayerMask.GetMask("Enemy"));
            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            return true; // Base checks (cooldown, cost, tags) are already handled
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var attackData = data as TowerNormalAttackData;
            if (attackData == null) return;

            var owner = asc.GetData().Owner;
            if (owner == null) return;

            // Get targets from EnemyManager
            var targets = enemyManager.GetEnemiesInRange(owner.position, attackData.attackRange, LayerMask.GetMask("Enemy"));
            if (targets == null || targets.Count == 0) return;
            
            // Limit to maxTargets
            int targetCount = Mathf.Min(attackData.maxTargets, targets.Count);
            
            debug.Log($"Tower Normal Attack fired at {targetCount} enemies!", Color.yellow);

            // Fire at each target
            for (int i = 0; i < targetCount; i++)
            {
                var target = targets[i];
                if (target == null) continue;

                FireAtTarget(owner, target, attackData, asc);
            }

            // NOTE: Don't end ability here - let the system handle it after cooldown is applied
            // asc.EndAbility(attackData);
        }

        private void FireAtTarget(Transform owner, Transform target, TowerNormalAttackData attackData, AbilitySystemComponent asc)
        {
            // Spawn muzzle effect
            if (attackData.muzzleEffect != null)
            {
                Object.Instantiate(attackData.muzzleEffect, owner.position, Quaternion.identity);
            }

            // Spawn projectile
            if (attackData.projectilePrefab != null)
            {
                Vector3 direction = (target.position - owner.position).normalized;
                var projectile = Object.Instantiate(
                    attackData.projectilePrefab,
                    owner.position + direction * 0.5f, // Offset from tower
                    Quaternion.LookRotation(direction)
                );

                // Setup projectile movement
                var rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * attackData.projectileSpeed;
                }

                // TODO: Add projectile component that deals damage on hit
                // For now, destroy after lifetime
                Object.Destroy(projectile, attackData.projectileLifetime);
            }
            else
            {
                // If no projectile, apply damage instantly
                // TODO: Get target's AbilitySystemComponent and apply damage effect
                debug.Log($"Hit target {target.name} for {attackData.damage} damage!", Color.red);
            }
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Clean up if needed
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            debug.Log("Tower Normal Attack cancelled", Color.yellow);
        }
    }
}
