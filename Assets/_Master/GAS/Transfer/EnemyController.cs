using FD.Ability;
using FD.Data;
using FD.Views;
using GAS;
using UnityEngine;
using VContainer.Unity;

namespace FD
{
    public readonly struct EventEnemyDestroyed
    {
        public readonly string EnemyId;
        public readonly Vector3 Position;
        public EventEnemyDestroyed(string enemyId, Vector3 position)
        {
            EnemyId = enemyId;
            Position = position;
        }
    }

    public class EnemyController: IAbilitySystemComponent
    {
        private readonly AbilitySystemComponent acs;
        private readonly IDebugService debug;
        private readonly IEventBus eventBus;
        private readonly IPoolManager poolManager;
        private readonly FDAttributeSet attributeSet = new FDAttributeSet();
        private EnemyData enemyData;
        private EnemyView enemyView;
        private string id;
        
        // Path following
        private Transform[] pathPoints;
        private int currentPathIndex = 0;
        
        public string Id => id;
        public Vector3 Position => enemyView != null ? enemyView.Position : Vector3.zero;
        public Transform Transform => enemyView != null ? enemyView.Transform : null;
        public GameObject GameObject => enemyView != null ? enemyView.GameObject : null;
        public int Layer => enemyView != null ? enemyView.Layer : 0;
        public bool IsActive => enemyView != null && enemyView.IsActive;
        public AbilitySystemComponent AbilitySystemComponent => acs;
#if UNITY_EDITOR
        // Public API for Editor debug tools
        
        public string DisplayName => $"Enemy #{currentCount} ({id.Substring(0, 8)})";
#endif

        private static int count;
        private int currentCount;

        public EnemyController(
            IDebugService debug,
            AbilitySystemComponent acs,
            IEventBus eventBus,
            IPoolManager poolManager)
        {
            this.debug = debug;
            this.acs = acs;
            this.eventBus = eventBus;
            this.poolManager = poolManager;
            id = System.Guid.NewGuid().ToString();
        }

        public void OnSetup(EnemyView enemyView, EnemyData enemyData)
        {
            this.enemyData = enemyData;
            this.enemyView = enemyView;
            currentCount = ++count;
            this.enemyView.ownerASC = this.acs; // Set owner for AbilitySystemComponent access
            acs.InitOwner(this.enemyView.transform);
            // Initialize ASC with enemy stats if needed
            acs.InitializeAttributeSet(attributeSet);
            // GrantAbilities() if enemies have abilities
        }

        public void Tick()
        {
            acs.Tick();
            
            // Enemy AI logic here (movement, targeting, etc.)
            UpdatePathFollowing();
        }
        
        public void SetPath(Transform[] points)
        {
            pathPoints = points;
            currentPathIndex = 0;
        }
        
        private void UpdatePathFollowing()
        {
            if (pathPoints == null || pathPoints.Length == 0 || enemyView == null || enemyData == null)
            {
                return;
            }
            
            if (currentPathIndex >= pathPoints.Length)
            {
                return; // Reached end of path
            }
            
            Transform targetPoint = pathPoints[currentPathIndex];
            if (targetPoint == null)
            {
                currentPathIndex++;
                return;
            }
            
            // Move towards current path point
            Vector3 direction = (targetPoint.position - Position).normalized;
            float distance = Vector3.Distance(Position, targetPoint.position);
            
            // Check if reached waypoint
            if (distance <= enemyData.WaypointThreshold)
            {
                currentPathIndex++;
            }
            else
            {
                // Move towards waypoint
                Vector3 newPosition = Position + direction * enemyData.MoveSpeed * Time.deltaTime;
                enemyView.UpdatePosition(newPosition);
            }
        }

        public void Destroy()
        {
            debug.Log($"EnemyController {id} is being destroyed!", Color.red);
            poolManager.Despawn(enemyView);
            eventBus.Publish(new EventEnemyDestroyed(id, Position));
        }
    }
}
