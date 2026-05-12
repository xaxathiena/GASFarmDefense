using FD.Ability;
using VContainer.Unity;
using FD.Abilities;
using UnityEngine;
using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;

namespace FD.Ability
{
    /// <summary>
    /// App-specific GAS Initializer. 
    /// Registers all project-specific ability data-to-behaviour mappings.
    /// </summary>
    public class AppGASInitializer : IStartable
    {
        private readonly AbilityBehaviourRegistry _registry;

        public AppGASInitializer(AbilityBehaviourRegistry registry)
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
                        
            // Add more abilities here as you create them
        }
    }
}



