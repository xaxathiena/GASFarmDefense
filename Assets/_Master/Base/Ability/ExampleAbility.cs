using UnityEngine;

namespace _Master.Base.Ability
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
        
        protected override void OnAbilityActivated()
        {
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
            EndAbility();
        }
        
        protected override void OnAbilityEnded()
        {
            Debug.Log($"{abilityName} ended!");
        }
        
        protected override void OnAbilityCancelled()
        {
            Debug.Log($"{abilityName} cancelled!");
        }
    }
}
