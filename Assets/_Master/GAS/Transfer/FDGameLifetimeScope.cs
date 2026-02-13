using FD.Abilities;
using FD.Controllers;
using FD.Data;
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
            
            // GAS System - Logic is Singleton (shared), Data is per-instance
            builder.Register<AbilitySystemLogic>(Lifetime.Singleton);
            builder.Register<GameplayAbilityLogic>(Lifetime.Singleton);
            builder.Register<AbilityBehaviourRegistry>(Lifetime.Singleton);
            builder.Register<AbilitySystemComponent>(Lifetime.Transient);
            
            // Ability Behaviours - Register all custom ability behaviours here
            builder.Register<FireballAbilityBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
            builder.Register<SlowBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
            
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<FDBattleSceneSetting>();

            builder.RegisterEntryPoint<FDBattleManager>(Lifetime.Singleton);
            builder.RegisterEntryPoint<DebugService>(Lifetime.Singleton).As<IDebugService>();
            builder.RegisterEntryPoint<GASInitializer>(Lifetime.Singleton);
            
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
}

