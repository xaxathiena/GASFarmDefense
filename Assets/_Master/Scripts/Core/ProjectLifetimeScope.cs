using Abel.TranHuongDao.Core;
using GASFarmDefense.Modules.Addressables;
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
            // Register GameUIManager
            if (gameUIManager != null)
            {
                builder.RegisterComponent(gameUIManager).AsSelf().As<GASFarmDefense.UIToolkit.Core.UIManager>();
            }

            // Register SceneLoaderService
            builder.Register<SceneLoaderService>(Lifetime.Singleton);

            // Register ConfigService
            builder.Register<ConfigService>(Lifetime.Singleton).AsImplementedInterfaces();

            // ── Addressables Module ───────────────────────────────────────────────
            builder.Register<AddressableService>(Lifetime.Singleton).As<IAddressableService>();
        }
    }
}
