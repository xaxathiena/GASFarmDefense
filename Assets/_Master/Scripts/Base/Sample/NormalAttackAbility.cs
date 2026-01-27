using UnityEngine;

namespace GAS.Sample
{
    /// <summary>
    /// Normal attack ability - Attacks nearest enemy
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_NormalAttack", menuName = "GAS/Sample/Normal Attack Ability")]
    public class NormalAttackAbility : GameplayAbility
    {
        [Header("Normal Attack Settings")]
        [Tooltip("Damage to deal")]
        public float damage = 10f;
        
        [Tooltip("Attack range")]
        public float attackRange = 3f;
        
        [Tooltip("Layers to target")]
        public LayerMask enemyLayers;
        
        [Tooltip("Damage effect to apply")]
        public GameplayEffect damageEffect;
        
        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var owner = GetAbilityOwner(asc);
            if (owner == null)
            {
                EndAbility(asc);
                return;
            }

            // Find nearest enemy
            Collider[] enemies = Physics.OverlapSphere(owner.transform.position, attackRange, enemyLayers);
            
            if (enemies.Length == 0)
            {
                Debug.Log($"{owner.name}: No enemies in range!");
                EndAbility(asc);
                return;
            }
            
            // Find closest
            Collider nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                if (enemy.gameObject == owner)
                    continue;
                    
                float distance = Vector3.Distance(owner.transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            if (nearestEnemy != null)
            {
                // Get target ASC
                var targetASC = nearestEnemy.GetComponent<AbilitySystemComponent>();
                
                if (targetASC != null)
                {
                    // Apply damage effect
                    if (damageEffect != null)
                    {
                        float effectLevel = spec?.Level ?? 1f;
                        asc.ApplyGameplayEffectToTarget(damageEffect, targetASC, asc, effectLevel);
                        Debug.Log($"{owner.name} attacked {nearestEnemy.name} for {damage} damage!");
                    }
                    else
                    {
                        // Direct damage
                        var targetAttributes = targetASC.AttributeSet.GetAttribute(EGameplayAttributeType.Health);
                        if (targetAttributes != null)
                        {
                            targetAttributes.ModifyCurrentValue(-damage);
                            Debug.Log($"{owner.name} attacked {nearestEnemy.name} for {damage} damage!");
                        }
                    }
                }
            }
            
            EndAbility(asc);
        }
    }
}
