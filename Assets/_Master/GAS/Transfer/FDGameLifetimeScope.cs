using FD.Controllers;
using FD.Data;
using VContainer;
using VContainer.Unity;
namespace FD
{
    public class FDGameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PoolManager>(Lifetime.Singleton).As<IPoolManager>();
            builder.RegisterComponentInHierarchy<FDBattleSceneSetting>();
            builder.Register<FDTowerControllerFactory>(Lifetime.Singleton);
            builder.Register<TowerController>(Lifetime.Transient);
            builder.RegisterEntryPoint<FDBattleManager>(Lifetime.Singleton);
            builder.RegisterEntryPoint<DebugService>(Lifetime.Singleton).As<IDebugService>();
        }
    }
    public class FDTowerControllerFactory
    {
        private readonly FDBattleSceneSetting fDBattleScene;
        private IObjectResolver container;
        public FDTowerControllerFactory(IObjectResolver container, FDBattleSceneSetting fDBattleScene)
        {
            this.container = container;
            this.fDBattleScene = fDBattleScene; 
        }
        public TowerController Create(TowerView towerView, TowerData towerData)
        {
            container.Resolve<TowerController>();
            return null;
        }
    }
}

