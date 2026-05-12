using Abel.GAS.Abilities;
using Abel.GAS.Effects;
using VContainer.Unity;
using UnityEngine;

namespace Abel.GAS.Samples.OrbCombat
{
    public class OrbGameInitializer : IStartable
    {
        private readonly AbilityBehaviourRegistry _registry;
        private readonly GameplayEffectService _effectService;

        public OrbGameInitializer(AbilityBehaviourRegistry registry, GameplayEffectService effectService)
        {
            _registry = registry;
            _effectService = effectService;
        }

        public void Start()
        {
            // Register behaviors for OrbAbilityData
            _registry.Register<OrbAbilityData>(new ApplyEffectAbilityBehaviour());
            
            // Note: If we want a specific behavior for Fireball, we might need a specific Data class 
            // OR use a different registration strategy.
            // For now, let's keep it simple.
            
            Debug.Log("[OrbCombat] GAS Behaviours registered.");
        }
    }
}
