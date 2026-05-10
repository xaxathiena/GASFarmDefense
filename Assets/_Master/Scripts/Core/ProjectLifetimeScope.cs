using Abel.TowerDefense.DebugTools;
using Abel.TranHuongDao.Core;
using GASFarmDefense.Modules.Addressables;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameUIManager gameUIManager;

        protected override void Awake()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Managers as global singletons
            builder.Register<ViewManager>(Lifetime.Singleton).AsSelf();
            builder.Register<PopupManager>(Lifetime.Singleton).AsSelf();

            // Register GameUIManager
            if (gameUIManager != null)
            {
                builder.RegisterComponent(gameUIManager)
                    .AsSelf()
                    .As<UIManager>();
            }

            // Register Essential Services
            builder.Register<SceneLoaderService>(Lifetime.Singleton).AsSelf();
            builder.Register<ConfigService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<AddressableService>(Lifetime.Singleton).As<IAddressableService>();
        }
    }
}
