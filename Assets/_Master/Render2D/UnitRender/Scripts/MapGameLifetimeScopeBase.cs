using UnityEngine;
using VContainer;
using VContainer.Unity;
using System.Collections.Generic;
using GASFarmDefense.UIToolkit.Core;
using Abel.TranHuongDao.Core;
using GASFarmDefense.UIToolkit.TranHuongDao;

namespace Abel.TowerDefense
{
    /// <summary>
    /// Base class for map-specific LifetimeScopes that require independent UI and core services.
    /// This ensures the map can run standalone or within the main game.
    /// </summary>
    public class MapGameLifetimeScopeBase : GameLifetimeScopeTDBase
    {
        [Header("Map UI Configuration")]
        [SerializeField] protected UnityEngine.UIElements.VisualTreeAsset _hudUxml;
        [SerializeField] protected List<MapPopupConfig> _mapPopups = new List<MapPopupConfig>();

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder); // GameLifetimeScopeTDBase registrations

            // ── Essential Independent Services ────────────────────────────────────
            builder.Register<ViewManager>(Lifetime.Singleton);
            builder.Register<PopupManager>(Lifetime.Singleton);
            builder.Register<SceneLoaderService>(Lifetime.Singleton);
            
            // Note: ProjectLifetimeScope also registers this, but we allow override for independence
            builder.RegisterEntryPoint<Abel.TranHuongDao.Core.ConfigService>(Lifetime.Singleton)
                   .As<Abel.TranHuongDao.Core.IConfigService>();

            // ── UI Setup Entry Point ──────────────────────────────────────────────
            builder.RegisterEntryPoint<MapUISetupProvider>(Lifetime.Singleton)
                   .WithParameter("hudUxml", _hudUxml)
               .WithParameter("mapPopups", _mapPopups);
        }
    }
}
