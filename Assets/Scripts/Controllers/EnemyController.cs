using System;
using UnityEngine;
using FD.Data;
using FD.Services;
using FD.Views;
using FD.Events;

namespace FD.Controllers
{
    /// <summary>
    /// Enemy controller - Kết nối Data, Logic, và View
    /// ✅ Nhận tất cả dependencies qua constructor injection
    /// ✅ Không có Unity dependencies trực tiếp (chỉ qua View)
    /// ✅ Testable - có thể mock tất cả dependencies
    /// 
    /// NOTE: Implements ITickable interface - sẽ được VContainer gọi mỗi frame
    /// Tạm thời chưa implement ITickable vì cần VContainer package
    /// </summary>
    public class EnemyController : IEnemy, IDisposable
    {
        // Dependencies - Injected qua constructor
        private readonly IEnemyMovementService _movementService;
        private readonly IEnemyAIService _aiService;
        private readonly IEnemyRegistry _registry;
        private readonly IGameplayEventBus _eventBus;
        
        // Data
        private readonly EnemyData _config;
        private readonly EnemyState _state;
        
        // View reference
        private readonly EnemyView _view;
        
        // IEnemy implementation
        public Transform Transform => _view.Transform;
        public Vector3 Position => _state.CurrentPosition;
        public GameObject GameObject => _view.GameObject;
        public int Layer => _view.Layer;
        public bool IsActive => _state.IsActive && _view.IsActive;
        public bool IsAlive => _state.IsAlive;
        
        // Constructor - VContainer auto-inject
        public EnemyController(
            EnemyView view,
            EnemyData config,
            IEnemyMovementService movementService,
            IEnemyAIService aiService,
            IEnemyRegistry registry,
            IGameplayEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _movementService = movementService ?? throw new ArgumentNullException(nameof(movementService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            // Initialize state
            _state = new EnemyState
            {
                CurrentPosition = view.Position,
                IsAlive = true,
                IsActive = true
            };
            
            // Subscribe view events
            _view.OnSpawned += OnViewSpawned;
            _view.OnDespawned += OnViewDespawned;
            _view.OnDestroyed += OnViewDestroyed;
        }
        
        // Lifecycle handlers
        private void OnViewSpawned(EnemyView view)
        {
            _registry.Register(this);
            _state.IsActive = true;
            
            // Publish event
            _eventBus.Publish(new EnemySpawnedEvent(this));
        }
        
        private void OnViewDespawned(EnemyView view)
        {
            _registry.Unregister(this);
            _state.IsActive = false;
            
            _eventBus.Publish(new EnemyDespawnedEvent(this));
        }
        
        private void OnViewDestroyed(EnemyView view)
        {
            Dispose();
        }
        
        // Tick - Called every frame (by VContainer ITickable or manually)
        public void Tick()
        {
            if (!_state.IsAlive || !_state.IsActive)
                return;
            
            // Sync state with view
            _state.CurrentPosition = _view.Position;
            
            // AI decision
            var decision = _aiService.Decide(_state, _config);
            
            // Execute decision
            switch (decision)
            {
                case EnemyAIDecision.FollowPath:
                    HandleFollowPath();
                    break;
                    
                case EnemyAIDecision.MoveToTarget:
                    HandleMoveToTarget();
                    break;
                    
                case EnemyAIDecision.Attack:
                    HandleAttack();
                    break;
                    
                case EnemyAIDecision.Idle:
                    // Do nothing
                    break;
            }
            
            // Check path completion
            if (_state.PathPoints != null && _state.CurrentPathIndex >= _state.PathPoints.Length)
            {
                if (!_state.HasReachedPathEnd)
                {
                    _state.HasReachedPathEnd = true;
                    _eventBus.Publish(new EnemyReachedPathEndEvent(this));
                }
            }
        }
        
        private void HandleFollowPath()
        {
            // Movement service tính vị trí mới
            var nextPos = _movementService.CalculateNextPosition(_state, _config, Time.deltaTime);
            
            // Update view
            _view.UpdatePosition(nextPos);
            
            // Update state
            _state.CurrentPosition = nextPos;
            
            // Check waypoint
            int newIndex = _movementService.GetNextPathIndex(_state, nextPos, _config.WaypointThreshold);
            if (newIndex != _state.CurrentPathIndex)
            {
                _state.CurrentPathIndex = newIndex;
                _eventBus.Publish(new EnemyReachedWaypointEvent(this, newIndex));
            }
        }
        
        private void HandleMoveToTarget()
        {
            if (_state.CurrentTarget == null)
                return;
            
            // Simple move towards
            Vector3 direction = _movementService.CalculateDirection(_state.CurrentPosition, _state.CurrentTarget.position);
            Vector3 nextPos = _state.CurrentPosition + direction * _config.MoveSpeed * Time.deltaTime;
            
            _view.UpdatePosition(nextPos);
            _view.LookAt(_state.CurrentTarget.position);
            
            _state.CurrentPosition = nextPos;
        }
        
        private void HandleAttack()
        {
            _state.IsAttacking = true;
            _state.LastAttackTime = Time.time;
            
            // Publish attack event - Ability system sẽ handle
            _eventBus.Publish(new EnemyAttackEvent(this, _state.CurrentTarget));
            
            // Animation
            _view.PlayAnimation("Attack");
            
            _state.IsAttacking = false;
        }
        
        // Public API
        public void SetPath(Transform[] pathPoints)
        {
            _state.PathPoints = pathPoints;
            _state.CurrentPathIndex = 0;
            _state.HasReachedPathEnd = false;
        }
        
        public void SetTarget(Transform target)
        {
            _state.CurrentTarget = target;
        }
        
        public void TakeDamage(float amount)
        {
            if (!_state.IsAlive)
                return;
            
            // Publish damage event - AttributeSet sẽ handle
            _eventBus.Publish(new EnemyDamagedEvent(this, amount));
        }
        
        public void Kill()
        {
            if (!_state.IsAlive)
                return;
            
            _state.IsAlive = false;
            _state.IsActive = false;
            
            _registry.Unregister(this);
            _eventBus.Publish(new EnemyDiedEvent(this));
            
            // Destroy view sau delay (cho animation)
            _view.PlayAnimation("Death");
            _view.DestroyView();
        }
        
        // IDisposable
        public void Dispose()
        {
            _view.OnSpawned -= OnViewSpawned;
            _view.OnDespawned -= OnViewDespawned;
            _view.OnDestroyed -= OnViewDestroyed;
            
            _registry.Unregister(this);
        }
    }
}
