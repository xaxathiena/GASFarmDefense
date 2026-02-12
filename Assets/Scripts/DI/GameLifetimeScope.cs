using UnityEngine;
using VContainer;
using VContainer.Unity;
using FD.Services;
using FD.Events;
using FD.Controllers;
using FD.Data;
using FD.Views;
using FD.Spawners;
using GAS;

namespace FD.DI
{
    /// <summary>
    /// Root lifetime scope cho toàn bộ game
    /// Đăng ký tất cả core services với VContainer
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        [Header("Debug")]
        [SerializeField] private bool logRegistrations = true;
        
        protected override void Configure(IContainerBuilder builder)
        {
            if (logRegistrations)
                Debug.Log("[GameLifetimeScope] Configuring VContainer DI container...");
            
            // ===== CORE SERVICES =====
            
            // Event bus - Singleton
            builder.Register<IGameplayEventBus, GameplayEventBus>(Lifetime.Singleton);
            
            // Enemy registry - Singleton
            builder.Register<IEnemyRegistry, EnemyRegistry>(Lifetime.Singleton);
            
            // Tower registry - Singleton
            builder.Register<ITowerRegistry, TowerRegistry>(Lifetime.Singleton);
            
            // Movement services - Singleton (stateless)
            builder.Register<IEnemyMovementService, PathMovementService>(Lifetime.Singleton);
            
            // AI services - Singleton (stateless)
            builder.Register<IEnemyAIService, BasicEnemyAI>(Lifetime.Singleton);
            
            // ===== FACTORIES =====
            
            // Enemy controller factory - sử dụng delegate để tạo controller với view và data
            builder.Register<EnemyControllerFactory>(Lifetime.Singleton);
            builder.Register<EnemyController>(Lifetime.Transient);
            builder.Register<TowerController>(Lifetime.Transient);
            builder.Register<AbilitySystemComponent>(Lifetime.Transient);
            // ===== MONOBEHAVIOURS IN SCENE =====
            
            // Tự động inject vào tất cả MonoBehaviour components trong scene
            builder.RegisterComponentInHierarchy<EnemySpawner>();
            builder.RegisterComponentInHierarchy<EnemyWaveSpawner>();
            builder.RegisterComponentInHierarchy<TowerSpawner>();
            
            // ===== ENTRY POINTS =====
            
            // Game initialization
            builder.RegisterEntryPoint<GameInitializer>();
            
            if (logRegistrations)
                Debug.Log("[GameLifetimeScope] VContainer DI container configured successfully!");
        }
    }
    
    /// <summary>
    /// Factory cho EnemyController
    /// </summary>
    public class EnemyControllerFactory
    {
        private readonly IEnemyMovementService _movementService;
        private readonly IEnemyAIService _aiService;
        private readonly IEnemyRegistry _registry;
        private readonly IGameplayEventBus _eventBus;
        private readonly IObjectResolver container;
        
        public EnemyControllerFactory(IObjectResolver container,
            IEnemyMovementService movementService,
            IEnemyAIService aiService,
            IEnemyRegistry registry,
            IGameplayEventBus eventBus)
        {
            this.container = container;
            _movementService = movementService;
            _aiService = aiService;
            _registry = registry;
            _eventBus = eventBus;
        }
        
        public EnemyController Create(EnemyView view, EnemyData config)
        {
            var enemy = container.Resolve<EnemyController>();
            enemy.OnSetup(view, config);
            return enemy;
        }
    }
    
    /// <summary>
    /// Entry point cho game initialization
    /// </summary>
    public class GameInitializer : IStartable
    {
        private readonly IGameplayEventBus _eventBus;
        
        public GameInitializer(IGameplayEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        
        public void Start()
        {
            Debug.Log("[GameInitializer] Game started with VContainer DI!");
            SetupEventListeners();
        }
        
        private void SetupEventListeners()
        {
            // Example: Log all enemy deaths
            _eventBus.Subscribe<EnemyDiedEvent>(e =>
            {
                Debug.Log($"[GameInitializer] Enemy died at {e.Enemy.Position}");
            });
            
            // Log enemy spawns
            _eventBus.Subscribe<EnemySpawnedEvent>(e =>
            {
                Debug.Log($"[GameInitializer] Enemy spawned at {e.Enemy.Position}");
            });
        }
    }
}
