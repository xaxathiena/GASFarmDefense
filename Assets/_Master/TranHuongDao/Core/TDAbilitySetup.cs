using VContainer.Unity;
using GAS;
using Abel.TranHuongDao.Core.Abilities;

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
        private readonly IEnemyManager _enemyManager;
        private readonly IBulletManager _bulletManager;
        private readonly ITowerManager _towerManager;
        private readonly GameplayAbilityLogic _gameplayAbilityLogic;
        private readonly MinionManager _minionManager;
        private readonly FD.Modules.VFX.IVFXManager _vfxManager;

        public TDAbilitySetup(
            AbilityBehaviourRegistry registry,
            IEnemyManager enemyManager,
            IBulletManager bulletManager,
            ITowerManager towerManager,
            GameplayAbilityLogic gameplayAbilityLogic,
            MinionManager minionManager,
            FD.Modules.VFX.IVFXManager vfxManager)
        {
            _registry = registry;
            _enemyManager = enemyManager;
            _bulletManager = bulletManager;
            _towerManager = towerManager;
            _gameplayAbilityLogic = gameplayAbilityLogic;
            _minionManager = minionManager;
            _vfxManager = vfxManager;
        }

        public void Start()
        {
            // Register every new ability here.
            // Pattern: _registry.Register<DataType>(new BehaviourType(deps...))

            _registry.Register<TDTowerNormalAttackData>(
                new TDTowerNormalAttackBehaviour(_enemyManager, _bulletManager));

            _registry.Register<TDTowerSkillData>(
                new TDTowerSkillBehaviour(_enemyManager));
            _registry.Register<TD_MultiAttackData>(
                new TD_MultiAttackBehaviour(_enemyManager, _bulletManager));
            _registry.Register<TD_NormalAttackData>(
                new TD_NormalAttackBehaviour(_enemyManager, _bulletManager));
            _registry.Register<TD_BaseProcData>(
                new TD_BaseProcBehaviour(_enemyManager, _towerManager, _gameplayAbilityLogic));
            _registry.Register<TD_AuraData>(
                new TD_AuraBehaviour(_enemyManager));
            _registry.Register<TD_AoEApplyEffectAbilityData>(
                new TD_AoEApplyEffectAbilityBehaviour(_enemyManager));
            _registry.Register<TD_SummonAbilityData>( // Added new registration
                new TD_SummonAbilityBehaviour(_minionManager)); // Injected _minionManager
            _registry.Register<TD_InstantAoEAbilityData>(
                new TD_InstantAoEAbilityBehaviour(_enemyManager, _towerManager, _vfxManager));

            // Add new abilities below as the project grows:
            // _registry.Register<TDFrostTowerData>(new TDFrostTowerBehaviour(_enemyManager));
        }
    }
}
