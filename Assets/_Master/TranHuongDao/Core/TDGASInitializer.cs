using VContainer.Unity;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Registers ability behaviour type mappings for the TranHuongDao Tower Defense module.
    /// Must be wired via <c>RegisterEntryPoint&lt;TDGASInitializer&gt;</c> in
    /// <see cref="GameLifetimeScope"/> so that <see cref="Start"/> runs before any ability fires.
    ///
    /// Each line maps a data ScriptableObject type to its corresponding stateless behaviour:
    ///   <c>registry.RegisterBehaviourType(typeof(DataClass), typeof(BehaviourClass))</c>
    ///
    /// Add a new line here every time you create a new ability.
    /// </summary>
    public sealed class TDGASInitializer : IStartable
    {
        private readonly AbilityBehaviourRegistry _registry;

        public TDGASInitializer(AbilityBehaviourRegistry registry)
        {
            _registry = registry;
        }

        public void Start()
        {
            // ── Tower abilities ──────────────────────────────────────────────────
            _registry.RegisterBehaviourType(
                typeof(TDTowerNormalAttackData),
                typeof(TDTowerNormalAttackBehaviour));

            // Add more here as the project grows:
            // _registry.RegisterBehaviourType(typeof(TDFrostTowerData), typeof(TDFrostTowerBehaviour));
        }
    }
}
