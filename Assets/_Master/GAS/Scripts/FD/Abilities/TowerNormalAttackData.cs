using UnityEngine;
using GAS;

namespace FD.Abilities
{
    /// <summary>
    /// Data configuration for Tower Normal Attack ability - PURE DATA ONLY.
    /// Cooldown: 2 seconds
    /// Max targets: 2 enemies
    /// </summary>
    [CreateAssetMenu(fileName = "TowerNormalAttack", menuName = "GAS/Abilities/Tower Normal Attack")]
    public class TowerNormalAttackData : GameplayAbilityData
    {
        [Header("Attack Settings")]
        [Tooltip("Damage dealt to each target")]
        public GameplayEffect damageEffect;
        
        [Tooltip("Maximum number of targets to hit (default: 2)")]
        public int maxTargets = 2;
        
        [Tooltip("Attack range")]
        public float attackRange = 10f;
        
        [Header("Visual Effects")]
        [Tooltip("Projectile prefab to spawn")]
        public GameObject projectilePrefab;
        
        [Tooltip("Projectile speed")]
        public float projectileSpeed = 15f;
        
        [Tooltip("Projectile lifetime")]
        public float projectileLifetime = 2f;
        
        [Tooltip("Muzzle effect when shooting")]
        public GameObject muzzleEffect;
        
        [Tooltip("Hit effect on target")]
        public GameObject hitEffect;

    }
}
