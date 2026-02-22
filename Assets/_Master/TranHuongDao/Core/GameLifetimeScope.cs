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

        // ── Tower ability asset (drag the TDTowerNormalAttack SO here) ───────────
        [Header("Tower Abilities")]
        [SerializeField] private TDTowerNormalAttackData towerNormalAttackData;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder); // Registers Render2DService, GameRenderManager, UnitDebugger, and UnitRenderDatabase
            // ── Debug / Logging ──────────────────────────────────────────────────
            // DebugService implements IStartable + IDisposable; RegisterEntryPoint
            // wires those interfaces into VContainer's PlayerLoop automatically.
            builder.RegisterEntryPoint<DebugService>(Lifetime.Singleton).As<IDebugService>();

            // ── GAS – Singleton services (stateless logic, shared across all ASCs) ─
            builder.Register<GameplayEffectCalculationService>(Lifetime.Singleton);
            builder.Register<GameplayEffectService>           (Lifetime.Singleton);
            builder.Register<AbilityBehaviourRegistry>        (Lifetime.Singleton);
            builder.Register<GameplayAbilityLogic>            (Lifetime.Singleton);
            builder.Register<AbilitySystemLogic>              (Lifetime.Singleton);
            // AbilitySystemComponent is Transient: each enemy/tower gets its own instance.
            // EnemyManager resolves these via IObjectResolver.
            builder.Register<AbilitySystemComponent>(Lifetime.Transient);

            // ── Ability Behaviours (Singleton, stateless) ─────────────────────────
            // Register the tower attack behaviour so VContainer can inject it as
            // IAbilityBehaviour AND as the concrete type (needed by AbilityBehaviourRegistry).
            builder.Register<TDTowerNormalAttackBehaviour>(Lifetime.Singleton)
                   .AsSelf()
                   .As<IAbilityBehaviour>();

            // ── GAS Initializer — explicit behaviour→data mapping ──────────────────
            // Must run before any ability fires; IStartable fires it automatically.
            builder.RegisterEntryPoint<TDGASInitializer>(Lifetime.Singleton);
            // Render2DService: ITickable flushes dirty buffers once per frame
            builder.RegisterEntryPoint<Render2DService>(Lifetime.Singleton).As<IRender2DService>();

            // ── Tower ability SO (ScriptableObject → RegisterInstance) ────────────
            builder.RegisterInstance(towerNormalAttackData);

            // ── Game Systems ──────────────────────────────────────────────────────
            builder.RegisterEntryPoint<MapManager>(Lifetime.Singleton).As<IMapManager>();
            builder.Register<WaveManager>(Lifetime.Singleton).As<IWaveManager>();

            // EnemyManager: ITickable + IStartable + IDisposable exposed as IEnemyManager
            builder.RegisterEntryPoint<EnemyManager>(Lifetime.Singleton).As<IEnemyManager>();

            // TowerManager: ITickable + IStartable + IDisposable exposed as ITowerManager
            builder.RegisterEntryPoint<TowerManager>(Lifetime.Singleton).As<ITowerManager>();

            // BulletManager: ITickable + IDisposable exposed as IBulletManager
            builder.RegisterEntryPoint<BulletManager>(Lifetime.Singleton).As<IBulletManager>();
        }
    }
}
