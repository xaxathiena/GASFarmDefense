using FD.Abilities;
using FD.Data;
using FD.Views;
using GAS;
using VContainer;
using VContainer.Unity;
namespace FD
{
    public class FDGameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PoolManager>(Lifetime.Singleton).As<IPoolManager>();
            builder.Register<FDTowerFactory>(Lifetime.Singleton);
            builder.Register<TowerController>(Lifetime.Transient);
            builder.Register<FDEnemyFactory>(Lifetime.Singleton);
            builder.Register<EnemyController>(Lifetime.Transient);
            
            // GAS System - Logic is Singleton (shared), Data is per-instance
            builder.Register<AbilitySystemLogic>(Lifetime.Singleton);
            builder.Register<GameplayAbilityLogic>(Lifetime.Singleton);
            builder.Register<AbilityBehaviourRegistry>(Lifetime.Singleton);
            builder.Register<AbilitySystemComponent>(Lifetime.Transient);
            builder.Register<EnemyManager>(Lifetime.Singleton);
            
            // GameplayEffect Services - Stateless logic for effect calculation and application
            builder.Register<GameplayEffectCalculationService>(Lifetime.Singleton);
            builder.Register<GameplayEffectService>(Lifetime.Singleton);

            // Ability Behaviours - Register all custom ability behaviours here
            builder.Register<FireballAbilityBehaviour>(Lifetime.Singleton).AsSelf().As<IAbilityBehaviour>();
            builder.Register<SlowBehaviour>(Lifetime.Singleton).AsSelf().As<IAbilityBehaviour>();
            builder.Register<TowerNormalAttackBehaviour>(Lifetime.Singleton).AsSelf().As<IAbilityBehaviour>();
            
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<FDBattleSceneSetting>();

            // IMPORTANT: GASInitializer MUST run before FDBattleManager
            // to register ability behaviours before towers are created
            builder.RegisterEntryPoint<GASInitializer>(Lifetime.Singleton);
            builder.RegisterEntryPoint<FDBattleManager>(Lifetime.Singleton);
            
            builder.RegisterEntryPoint<DebugService>(Lifetime.Singleton).As<IDebugService>();
            
        }
    }
    public class FDTowerFactory
    {
        private readonly FDBattleSceneSetting fDBattleScene;
        private IObjectResolver container;
        public FDTowerFactory(IObjectResolver container, FDBattleSceneSetting fDBattleScene)
        {
            this.container = container;
            this.fDBattleScene = fDBattleScene; 
        }
        public TowerController Create(TowerView towerView, TowerData towerData)
        {
            var controller = container.Resolve<TowerController>();
            controller.OnSetup(towerView, towerData);
            return controller;
        }
    }
    
    public class FDEnemyFactory
    {
        private readonly FDBattleSceneSetting fDBattleScene;
        private readonly IObjectResolver container;
        
        public FDEnemyFactory(IObjectResolver container, FDBattleSceneSetting fDBattleScene)
        {
            this.container = container;
            this.fDBattleScene = fDBattleScene;
        }
        
        public EnemyController Create(EnemyView enemyView, EnemyData enemyData)
        {
            var controller = container.Resolve<EnemyController>();
            controller.OnSetup(enemyView, enemyData);
            return controller;
        }
    }
}
