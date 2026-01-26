using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Example ability implementation - Deal damage in area
    /// </summary>
    [CreateAssetMenu(fileName = "New Example Ability", menuName = "GAS/Abilities/Example Ability")]
    public class ExampleAbility : GameplayAbility
    {
        [Header("Example Ability Settings")]
        public float damageAmount = 10f;
        public float radius = 5f;
        public LayerMask targetLayers;
        
        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var owner = GetAbilityOwner(asc);
            if (owner == null)
            {
                EndAbility(asc);
                return;
            }

            Debug.Log($"{abilityName} activated by {owner.name}!");
            
            // Example: Deal damage to all enemies in radius
            Collider[] hitColliders = Physics.OverlapSphere(owner.transform.position, radius, targetLayers);
            
            foreach (var hitCollider in hitColliders)
            {
                // Apply damage logic here
                Debug.Log($"Hit {hitCollider.gameObject.name} for {damageAmount} damage!");
                
                // Example: You could get a health component and apply damage
                // var health = hitCollider.GetComponent<HealthComponent>();
                // if (health != null)
                //     health.TakeDamage(damageAmount);
            }
            
            // End ability immediately (instant cast)
            EndAbility(asc);
        }

        protected override void OnAbilityEnded(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            Debug.Log($"{abilityName} ended!");
        }

        protected override void OnAbilityCancelled(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            Debug.Log($"{abilityName} cancelled!");
        }
    }
}
