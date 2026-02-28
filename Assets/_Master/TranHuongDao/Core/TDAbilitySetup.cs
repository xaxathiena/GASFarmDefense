using VContainer.Unity;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Single source of truth for all Tower Defense ability registrations.
    ///
    /// To add a new ability: add ONE line here - no other file needs to change.
    /// Each behaviour is constructed here with its dependencies injected via this
    /// constructor, then pre-cached in AbilityBehaviourRegistry through
    /// Register&lt;TData&gt;. This removes the need for individual builder.Register
    /// calls in GameLifetimeScope when adding new abilities.
    /// </summary>
    public sealed class TDAbilitySetup : IStartable
    {
        private readonly AbilityBehaviourRegistry _registry;
        private readonly IEnemyManager            _enemyManager;
        private readonly IBulletManager           _bulletManager;

        public TDAbilitySetup(
            AbilityBehaviourRegistry registry,
            IEnemyManager            enemyManager,
            IBulletManager           bulletManager)
        {
            _registry      = registry;
            _enemyManager  = enemyManager;
            _bulletManager = bulletManager;
        }

        public void Start()
        {
            // Register every new ability here.
            // Pattern: _registry.Register<DataType>(new BehaviourType(deps...))

            _registry.Register<TDTowerNormalAttackData>(
                new TDTowerNormalAttackBehaviour(_enemyManager, _bulletManager));

            _registry.Register<TDTowerSkillData>(
                new TDTowerSkillBehaviour(_enemyManager));

            // Add new abilities below as the project grows:
            // _registry.Register<TDFrostTowerData>(new TDFrostTowerBehaviour(_enemyManager));
        }
    }
}
