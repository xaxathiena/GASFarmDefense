using VContainer.Unity;

namespace GAS
{
    /// <summary>
    /// Initializes GAS system by setting static logic references for ScriptableObjects.
    /// This is needed because ScriptableObjects can't use constructor injection.
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
            // Set static references for GameplayAbility ScriptableObjects
            GameplayAbility.SetLogic(abilityLogic);
            GameplayAbility.SetRegistry(behaviourRegistry);
        }
    }
}
