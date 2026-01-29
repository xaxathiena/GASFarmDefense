using GAS;
using UnityEngine;

namespace FD.Ability
{
    /// <summary>
    /// Example: Projectile ability that uses FD damage system with WC3 calculation.
    /// Demonstrates how to:
    /// 1. Inherit from FDGameplayAbility (not GameplayAbility)
    /// 2. Set damage type on the ability (Magic, Pierce, etc.)
    /// 3. Create FD context and apply effects with damage calculation
    /// </summary>
    [CreateAssetMenu(fileName = "FDProjectileAbility", menuName = "FD/Abilities/FD Projectile Ability")]
    public class FDProjectileAbility : FDGameplayAbility
    {
        [Header("Projectile Settings")]
        [Tooltip("Projectile prefab to spawn")]
        public GameObject projectilePrefab;
        [Tooltip("GameplayEffect to apply on hit")]
         public GameplayEffect gameplayEffect;
        [Tooltip("Projectile speed")]
        public float projectileSpeed = 10f;
        
        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            base.OnAbilityActivated(asc, spec);
            
            // Example: Get target from owner
            // In real implementation, this would come from targeting system
            var targetASC = FindTargetASC(asc);
            if (targetASC == null)
            {
                Debug.LogWarning($"[{abilityName}] No target found!");
                EndAbility(asc, spec);
                return;
            }
            
            // Apply effect with FD context
            if (gameplayEffect != null)
            {
                ApplyEffectWithContext(gameplayEffect, asc, targetASC, spec);
            }
            
            // Spawn projectile (optional - for visual feedback)
            if (projectilePrefab != null)
            {
                SpawnProjectile(asc, targetASC);
            }
            
            EndAbility(asc, spec);
        }
        
        private AbilitySystemComponent FindTargetASC(AbilitySystemComponent source)
        {
            // TODO: Implement targeting logic
            // For now, just find nearest enemy
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length > 0)
            {
                return enemies[0].GetComponent<AbilitySystemComponent>();
            }
            return null;
        }
        
        private void SpawnProjectile(AbilitySystemComponent source, AbilitySystemComponent target)
        {
            var sourcePos = source.transform.position;
            var targetPos = target.transform.position;
            
            var projectile = Instantiate(projectilePrefab, sourcePos, Quaternion.identity);
            
            // TODO: Setup projectile movement
            Debug.Log($"[{abilityName}] Spawned projectile from {source.name} to {target.name}");
        }
    }
}
