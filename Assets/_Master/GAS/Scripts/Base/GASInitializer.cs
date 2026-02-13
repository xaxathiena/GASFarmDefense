using VContainer.Unity;
using FD.Abilities;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Initializes GAS system by registering behaviour type mappings.
    /// ALL abilities must be registered here to work.
    /// </summary>
    public class GASInitializer : IStartable
    {
        private readonly AbilityBehaviourRegistry _registry;

        public GASInitializer(AbilityBehaviourRegistry registry)
        {
            _registry = registry;
        }

        public void Start()
        {
            // Register ALL ability behaviour type mappings here
            // Format: _registry.RegisterBehaviourType(typeof(DataClass), typeof(BehaviourClass));
            
            _registry.RegisterBehaviourType(typeof(FireballAbilityData), typeof(FireballAbilityBehaviour));
            _registry.RegisterBehaviourType(typeof(SlowData), typeof(SlowBehaviour));
            _registry.RegisterBehaviourType(typeof(TowerNormalAttackData), typeof(TowerNormalAttackBehaviour));
            
            Debug.Log("[GASInitializer] Registered 3 ability behaviours");
            
            // Add more abilities here as you create them
            // _registry.RegisterBehaviourType(typeof(HealAbilityData), typeof(HealAbilityBehaviour));
        }
    }
}
