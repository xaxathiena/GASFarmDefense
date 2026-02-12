using UnityEngine;
using GAS;

namespace FD.Abilities
{
    /// <summary>
    /// Behaviour logic for Slow ability.
    /// Implement ability-specific logic here.
    /// This class is a Singleton and should be stateless.
    /// </summary>
    public class SlowBehaviour : IAbilityBehaviour
    {
        // Inject any services you need via constructor
        private readonly IDebugService debug;
        
        public SlowBehaviour(IDebugService debug)
        {
            this.debug = debug;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Add custom activation checks here
            // Example: check target in range, check resources, etc.
            
            return true; // Base checks are already handled by GameplayAbilityLogic
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var slowData = data as SlowData;
            if (slowData == null)
            {
                Debug.LogError("Invalid data type for SlowBehaviour");
                return;
            }

            // Implement ability logic here
            // Example:
            // - Spawn projectiles
            // - Apply damage/healing
            // - Play VFX/SFX
            // - Trigger animations
            
            debug.Log($"Slow activated!", Color.cyan);
            
            // Example of ending ability immediately (for instant abilities)
            // asc.EndAbility(slowData);
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Clean up ability state here
            // Example: destroy projectiles, stop VFX, etc.
            
            debug.Log($"Slow ended", Color.gray);
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Handle cancellation here
            // Example: refund partial costs, interrupt animations, etc.
            
            debug.Log($"Slow cancelled", Color.yellow);
        }
    }
}
