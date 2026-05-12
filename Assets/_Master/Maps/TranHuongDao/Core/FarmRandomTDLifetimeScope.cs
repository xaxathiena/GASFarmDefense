using Abel.TowerDefense; // UnitDebugger
using Abel.TowerDefense.Config;    // UnitRenderDatabase
using Abel.TowerDefense.DebugTools;
using Abel.TowerDefense.Render;    // GameRenderManager
using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Root VContainer scope for the TranHuongDao Tower Defense scene.
    /// </summary>
    public class FarmRandomTDLifetimeScope : MapGameLifetimeScopeBase
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

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder); // Registers Render2DService, GameRenderManager, UnitDebugger, and UnitRenderDatabase

            // ── Event System ──────────────────────────────────────────────────────
            builder.Register<FD.EventBus>(Lifetime.Singleton).As<FD.IEventBus>().As<System.IDisposable>();

            // ── VFX System ────────────────────────────────────────────────────────
            builder.RegisterEntryPoint<FD.Modules.VFX.VFXManager>(Lifetime.Singleton).As<FD.Modules.VFX.IVFXManager>();

            // ── Debug / Logging ──────────────────────────────────────────────────
            builder.RegisterEntryPoint<DebugService>(Lifetime.Singleton).As<IDebugService>();

            // ── Floating Text System ──────────────────────────────────────────────

            builder.RegisterEntryPoint<Abel.TranHuongDao.Core.UI.FloatingTextManager>(Lifetime.Singleton).AsSelf();

            // ── GAS – Singleton services (stateless logic, shared across all ASCs) ─
            builder.Register<GameplayEffectCalculationService>(Lifetime.Singleton);
            builder.Register<GameplayEffectService>(Lifetime.Singleton);
            builder.Register<AbilityBehaviourRegistry>(Lifetime.Singleton);
            builder.Register<GameplayAbilityLogic>(Lifetime.Singleton);
            builder.Register<AbilitySystemLogic>(Lifetime.Singleton);
            builder.Register<AbilitySystemComponent>(Lifetime.Transient);

            // -- Ability Behaviours (Singleton, stateless) ---------------------------
            builder.RegisterEntryPoint<TDAbilitySetup>(Lifetime.Singleton);

            // Render2DService: ITickable flushes dirty buffers once per frame

            builder.RegisterEntryPoint<Render2DService>(Lifetime.Singleton).As<IRender2DService>();

            // ── Tower builder config — inject the inner plain-data class directly ──
            builder.RegisterInstance(towerBuilderConfigSO.config);

            // ── Game Systems ──────────────────────────────────────────────────────
            builder.RegisterComponent(mapLayoutManager)
                   .As<IMapLayoutManager>();

            builder.RegisterComponent(towerDragDropManager)
                   .As<ITickable>();

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

            // ── Modular Unit Logic & Minions ──────────────────────────────────────
            builder.Register<UnitLogicFactory>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MinionManager>(Lifetime.Singleton).AsSelf();

            // Register all concrete logic types for the factory to resolve
            builder.Register<StationaryAttackLogic>(Lifetime.Transient);
            builder.Register<PathFollowerLogic>(Lifetime.Transient);
            builder.Register<HomingSuicideLogic>(Lifetime.Transient);
            builder.Register<PetFollowerLogic>(Lifetime.Transient);

            // ── Economy & Farming ──────────────────────────────────────────
            builder.RegisterEntryPoint<TDEconomyService>(Lifetime.Singleton).AsSelf();
            builder.Register<TDHandService>(Lifetime.Singleton).AsSelf();
        }
    }
}


