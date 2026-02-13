using VContainer.Unity;
using FD.Abilities;

namespace GAS
{
    /// <summary>
    /// Initializes GAS system by registering behaviour type mappings.
    /// ALL abilities must be registered here to work.
    /// </summary>
    public class GASInitializer : IStartable
    {
        private readonly GameplayAbilityLogic abilityLogic;
        private readonly AbilityBehaviourRegistry behaviourRegistry;

        public GASInitializer(GameplayAbilityLogic abilityLogic, AbilityBehaviourRegistry behaviourRegistry)
        {
            this.abilityLogic = abilityLogic;
            this.behaviourRegistry = behaviourRegistry;
        }

        public void Start()
        {
            // Register ALL ability behaviour type mappings here
            // Format: abilityLogic.RegisterBehaviourType(typeof(DataClass), typeof(BehaviourClass));
            
            abilityLogic.RegisterBehaviourType(typeof(FireballAbilityData), typeof(FireballAbilityBehaviour));
            abilityLogic.RegisterBehaviourType(typeof(SlowData), typeof(SlowBehaviour));
            
            // Add more abilities here as you create them
            // abilityLogic.RegisterBehaviourType(typeof(HealAbilityData), typeof(HealAbilityBehaviour));
        }
    }
}
