using UnityEngine;
using GAS;

namespace FD.Abilities
{
    /// <summary>
    /// Behaviour logic for Fireball ability.
    /// This is a Singleton service that handles all Fireball activation logic.
    /// It's stateless and can be shared across all Fireball ability instances.
    /// </summary>
    public class FireballAbilityBehaviour : IAbilityBehaviour
    {
        private readonly IDebugService debug;
        
        public FireballAbilityBehaviour(IDebugService debug)
        {
            this.debug = debug;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var fireballData = data as FireballAbilityData;
            if (fireballData == null)
            {
                Debug.LogError("Invalid data type for FireballAbilityBehaviour");
                return false;
            }

            // Add custom checks here
            // Example: check if projectile prefab is assigned
            if (fireballData.projectilePrefab == null)
            {
                debug.Log("Fireball: No projectile prefab assigned!", Color.red);
                return false;
            }

            // Example: check if owner has a target
            // You can access owner's transform, components, etc.
            
            return true; // Base checks (cooldown, cost, tags) are already handled
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var fireballData = data as FireballAbilityData;
            if (fireballData == null) return;

            var owner = asc.GetData().Owner;
            
            debug.Log($"Fireball activated! Damage: {fireballData.damage}, Speed: {fireballData.projectileSpeed}", Color.cyan);

            // Spawn cast effect
            if (fireballData.castEffect != null)
            {
                Object.Instantiate(fireballData.castEffect, owner.position, Quaternion.identity);
            }

            // Spawn projectile
            if (fireballData.projectilePrefab != null)
            {
                var projectile = Object.Instantiate(
                    fireballData.projectilePrefab,
                    owner.position + owner.forward * 1f, // Offset from caster
                    Quaternion.LookRotation(owner.forward)
                );

                // Setup projectile logic (you can create a ProjectileComponent)
                var rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = owner.forward * fireballData.projectileSpeed;
                }

                // Destroy projectile after lifetime
                Object.Destroy(projectile, fireballData.projectileLifetime);
            }

            // For instant abilities (like projectile launch), end immediately
            asc.EndAbility(fireballData);
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            debug.Log("Fireball ended", Color.gray);
            
            // Clean up if needed
            // Example: clear any ongoing effects, stop VFX, etc.
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            debug.Log("Fireball cancelled", Color.yellow);
            
            // Handle cancellation
            // Example: refund partial cost, destroy incomplete projectiles, etc.
        }
    }
}
