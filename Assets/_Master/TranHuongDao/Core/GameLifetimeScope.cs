using UnityEngine;
using VContainer;
using VContainer.Unity;
using GAS;
using Abel.TowerDefense.Render;    // GameRenderManager
using Abel.TowerDefense.Config;    // UnitRenderDatabase
using Abel.TowerDefense.DebugTools;
using Abel.TowerDefense; // UnitDebugger

// Recompile trigger: Feb 22 2026

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Root VContainer scope for the TranHuongDao Tower Defense scene.
    /// </summary>
    public class GameLifetimeScope : GameLifetimeScopeTDBase
    {


        // ── Tower builder config (drag the TowerBuilderConfigSO asset here) ─────
        [Header("Tower Builder")]
        [SerializeField] private TowerBuilderConfigSO towerBuilderConfigSO;

        // ── Map layout (place the MapLayoutManager MonoBehaviour in the scene) ────
        [Header("Map Layout")]
        [SerializeField] private MapLayoutManager mapLayoutManager;

        // ── Drag-and-drop system (scene MonoBehaviour with the preview SpriteRenderer) ──
        [Header("Tower Drag & Drop")]
        [SerializeField] private TowerDragDropManager towerDragDropManager;

        [Header("Tower Selection UI")]
        [SerializeField] private UnitSelectionUIView towerSelectionUIView;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder); // Registers Render2DService, GameRenderManager, UnitDebugger, and UnitRenderDatabase

            // ── Event System ──────────────────────────────────────────────────────
            builder.Register<FD.EventBus>(Lifetime.Singleton).As<FD.IEventBus>().As<System.IDisposable>();

            // ── VFX System ────────────────────────────────────────────────────────
            builder.RegisterEntryPoint<FD.Modules.VFX.VFXManager>(Lifetime.Singleton).As<FD.Modules.VFX.IVFXManager>();

            // ── Debug / Logging ──────────────────────────────────────────────────
            // DebugService implements IStartable + IDisposable; RegisterEntryPoint
            // wires those interfaces into VContainer's PlayerLoop automatically.
            builder.RegisterEntryPoint<DebugService>(Lifetime.Singleton).As<IDebugService>();
            builder.RegisterEntryPoint<ConfigService>(Lifetime.Singleton).As<IConfigService>();
            // ── GAS – Singleton services (stateless logic, shared across all ASCs) ─
            builder.Register<GameplayEffectCalculationService>(Lifetime.Singleton);
            builder.Register<GameplayEffectService>(Lifetime.Singleton);
            builder.Register<AbilityBehaviourRegistry>(Lifetime.Singleton);
            builder.Register<GameplayAbilityLogic>(Lifetime.Singleton);
            builder.Register<AbilitySystemLogic>(Lifetime.Singleton);
            // AbilitySystemComponent is Transient: each enemy/tower gets its own instance.
            // EnemyManager resolves these via IObjectResolver.
            builder.Register<AbilitySystemComponent>(Lifetime.Transient);

            // -- Ability Behaviours (Singleton, stateless) ---------------------------
            // Behaviours are constructed and registered inside TDAbilitySetup.Start().
            // To add a new ability: only TDAbilitySetup.cs needs to change.

            // GAS ability setup -- constructs + registers all behaviour instances.
            // Must run before any ability fires; IStartable fires it automatically.
            builder.RegisterEntryPoint<TDAbilitySetup>(Lifetime.Singleton);
            // Render2DService: ITickable flushes dirty buffers once per frame
            builder.RegisterEntryPoint<Render2DService>(Lifetime.Singleton).As<IRender2DService>();

            // ── Tower builder config — inject the inner plain-data class directly ──
            // RegisterInstance pins the already-created TowerBuilderConfig value so any
            // class that declares a constructor parameter of type TowerBuilderConfig
            // will receive this instance automatically.
            builder.RegisterInstance(towerBuilderConfigSO.config);

            // ── Game Systems ──────────────────────────────────────────────────────
            // MapLayoutManager is a MonoBehaviour; use RegisterComponent to bind the
            // scene instance so VContainer injects it as both interface types.
            // Single MapLayoutManager instance satisfies all map-related contracts.
            builder.RegisterComponent(mapLayoutManager)
                   .As<IMapLayoutManager>();

            // TowerDragDropManager is a MonoBehaviour; RegisterComponent + As<ITickable>
            // ensures VContainer calls its Tick() every frame via the PlayerLoop.
            // VContainer also calls [Inject] Construct() to supply IMapLayoutManager.
            builder.RegisterComponent(towerDragDropManager)
                   .As<ITickable>();
            builder.RegisterComponent(towerSelectionUIView)
                   .AsSelf(); // Inject into TowerSelectionManager, resolved by TowerManager

            // EnemyManager: ITickable + IStartable + IDisposable exposed as IEnemyManager
            builder.RegisterEntryPoint<EnemyManager>(Lifetime.Singleton).As<IEnemyManager>();

            // TowerManager: also exposed as ITowerSpawner so TowerDragDropManager can
            // call SpawnTower() without depending on the full ITowerManager contract.
            builder.RegisterEntryPoint<TowerManager>(Lifetime.Singleton)
                   .As<ITowerManager>()
                   .As<ITowerSpawner>();

            // BulletManager: ITickable + IDisposable exposed as IBulletManager
            builder.RegisterEntryPoint<BulletManager>(Lifetime.Singleton).As<IBulletManager>();
            builder.RegisterEntryPoint<WaveManager>(Lifetime.Singleton).As<IWaveManager>();
            builder.RegisterEntryPoint<InstanceIDService>(Lifetime.Singleton).As<IInstanceIDService>();
            builder.RegisterEntryPoint<TowerSelectionManager>(Lifetime.Singleton).AsSelf();
        }
    }
}
