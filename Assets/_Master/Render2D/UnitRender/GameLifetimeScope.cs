using VContainer;
using VContainer.Unity;
using UnityEngine;
using Abel.TowerDefense.Render;
using Abel.TowerDefense.InputSystem;
using Abel.TowerDefense.DebugTools;

namespace Abel.TowerDefense
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameUnitManager gameManager; // drag GameObject GameUnitManager here (manages unit groups, spawning, updating)
        [SerializeField] private InputCreateUnit inputHandler; // drag GameObject InputCreateUnit here (handles input for spawning units)
        [SerializeField] private UnitDebugger unitDebugger; // drag GameObject UnitDebugger here (optional, for debugging)

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Manager (Component in Scene)
            builder.RegisterComponent(gameManager);
            // Register Input Handler (Component in Scene)
            builder.RegisterComponent(inputHandler);
            // Register Debugger (Component in Scene)
            builder.RegisterComponent(unitDebugger);
            // Auto-inject dependencies into scene components
            // InputCreateUnit and UnitDebugger will automatically receive injected dependencies
            // Enable "Auto Inject Game Objects" in the Inspector for automatic property injection
        }
    }
}