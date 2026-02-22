using VContainer;
using VContainer.Unity;
using UnityEngine;
using Abel.TowerDefense.Render;
using Abel.TowerDefense.DebugTools;
using Abel.TowerDefense.Config;

namespace Abel.TowerDefense
{
    public class GameLifetimeScopeTDBase : LifetimeScope
    {
        [SerializeField] protected UnitRenderDatabase gameDatabase;
        [SerializeField] protected GameRenderManager gameManager;
        [SerializeField] protected UnitDebugger unitDebugger; // Optional, for debugging
        [SerializeField] protected bool showUnitDebugger = true; // Control visibility of UnitDebugger
        protected override void Configure(IContainerBuilder builder)
        {
            // Register Data (Singleton)
            builder.RegisterInstance(gameDatabase);
            // Register Systems (Components in Scene)
            builder.RegisterComponent(gameManager);
            builder.RegisterInstance(new UnitRenderGameSettings
            {
                IsDebugMode = showUnitDebugger
            });
            builder.RegisterComponent(unitDebugger); // Optional, for debugging
        }
    }
}