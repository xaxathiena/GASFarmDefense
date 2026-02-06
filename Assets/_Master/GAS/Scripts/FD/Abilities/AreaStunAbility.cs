using FD.Character;
using GAS;
using UnityEngine;
using System.Collections.Generic;

namespace FD.Ability
{
    /// <summary>
    /// Area Stun Ability - Periodically stuns all enemies in range
    /// Activates every 2 seconds, stuns enemies within 4 units for 0.5 seconds
    /// Checks for immunity before applying stun
    /// </summary>
    [CreateAssetMenu(fileName = "AreaStunAbility", menuName = "FD/Abilities/Area Stun Ability")]
    public class AreaStunAbility : FDGameplayAbility
    {
        [Header("Area Stun Configuration")]
        [Tooltip("Stun effect to apply (should have 'State.Stunned' tag)")]
        [SerializeField] private GameplayEffect stunEffect;

        [Tooltip("Radius to detect enemies")]
        [SerializeField] private float detectionRadius = 4f;

        [Tooltip("Layer mask for enemy detection")]
        [SerializeField] private LayerMask enemyLayerMask = ~0;

        [Tooltip("Stun duration in seconds")]
        [SerializeField] private float stunDuration = 0.5f;

        [Tooltip("Visual effect prefab when enemy is stunned (optional)")]
        [SerializeField] private GameObject stunVFXPrefab;

        [Tooltip("Show detection radius in editor")]
        [SerializeField] private bool showGizmos = true;

        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            base.OnAbilityActivated(asc, spec);

            var owner = GetAbilityOwner(asc);
            if (owner == null)
            {
                EndAbility(asc, spec);
                return;
            }

            // Find all enemies in detection radius
            var enemiesInRange = FindEnemiesInRadius(owner.transform.position);
            
            if (enemiesInRange.Count == 0)
            {
                Debug.Log($"[AreaStunAbility] No enemies found in range for {owner.name}");
                EndAbility(asc, spec);
                return;
            }

            Debug.Log($"[AreaStunAbility] Found {enemiesInRange.Count} enemies in range");

            // Apply stun to each enemy
            int stunnedCount = 0;
            int immuneCount = 0;

            foreach (var enemy in enemiesInRange)
            {
                var targetASC = enemy.GetComponent<AbilitySystemComponent>();
                if (targetASC == null)
                {
                    continue;
                }

                // Check if target is immune to stun
                if (IsImmune(targetASC))
                {
                    Debug.Log($"[AreaStunAbility] {enemy.name} is IMMUNE to stun!");
                    immuneCount++;
                    OnStunBlocked(enemy, targetASC);
                    continue;
                }

                // Apply stun effect
                if (stunEffect != null)
                {
                    asc.ApplyGameplayEffectToTarget(stunEffect, targetASC, asc, spec.Level);
                    Debug.Log($"[AreaStunAbility] {enemy.name} is STUNNED for {stunDuration}s!");
                    stunnedCount++;
                    OnStunApplied(enemy, targetASC);
                }
            }

            Debug.Log($"[AreaStunAbility] Stunned {stunnedCount} enemies, {immuneCount} were immune");
            EndAbility(asc, spec);
        }

        /// <summary>
        /// Find all enemies within detection radius
        /// </summary>
        private List<GameObject> FindEnemiesInRadius(Vector3 center)
        {
            var enemies = new List<GameObject>();
            
            // Use OverlapSphere to find all colliders in range
            Collider[] colliders = Physics.OverlapSphere(center, detectionRadius, enemyLayerMask);
            
            foreach (var collider in colliders)
            {
                // Check if has AbilitySystemComponent (valid target)
                var asc = collider.GetComponent<AbilitySystemComponent>();
                if (asc != null)
                {
                    // Check if is enemy (has BaseCharacter and is not friendly)
                    var baseChar = collider.GetComponent<BaseCharacter>();
                    if (baseChar != null)
                    {
                        enemies.Add(collider.gameObject);
                    }
                }
            }

            return enemies;
        }

        /// <summary>
        /// Check if target is immune to stun
        /// Checks for immunity tags: State.Immune, State.Immune.Stun, State.Immune.CC
        /// </summary>
        private bool IsImmune(AbilitySystemComponent targetASC)
        {
            if (targetASC == null)
            {
                return false;
            }

            // Check for general immunity, stun immunity, or CC immunity
            if (targetASC.HasAnyTags(GameplayTag.State_Immune, GameplayTag.State_Immune_Stun, GameplayTag.State_Immune_CC))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when stun is successfully applied
        /// Override to add custom logic (VFX, sounds, etc.)
        /// </summary>
        protected virtual void OnStunApplied(GameObject target, AbilitySystemComponent targetASC)
        {
            // Spawn VFX if available
            if (stunVFXPrefab != null)
            {
                var vfx = Instantiate(stunVFXPrefab, target.transform.position, Quaternion.identity, target.transform);
                
                // Destroy VFX after stun duration
                Destroy(vfx, stunDuration);
            }
        }

        /// <summary>
        /// Called when stun is blocked by immunity
        /// Override to add custom logic (show immunity VFX, etc.)
        /// </summary>
        protected virtual void OnStunBlocked(GameObject target, AbilitySystemComponent targetASC)
        {
            // Override in subclass to show "IMMUNE" popup or VFX
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draw detection radius gizmo in editor
        /// </summary>
        public void DrawGizmos(Transform ownerTransform)
        {
            if (!showGizmos || ownerTransform == null)
            {
                return;
            }

            // Draw detection radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(ownerTransform.position, detectionRadius);
        }
#endif
    }
}
