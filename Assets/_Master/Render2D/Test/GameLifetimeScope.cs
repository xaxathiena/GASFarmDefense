using Abel.TowerDefense.Config;
using Abel.TowerDefense.DebugTools;
using Abel.TowerDefense.Render;
using UnityEngine;
using VContainer;
using VContainer.Unity;
namespace Abel.TowerDefense.Test
{
    public class GameLifetimeScope : GameLifetimeScopeTDBase
    {
        [SerializeField] private TestUnitManager testManager;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder); // Register base dependencies
            builder.RegisterComponent(testManager);

            // Make sure your Scope has "Auto Inject Game Objects" enabled in Inspector!
        }
    }
}