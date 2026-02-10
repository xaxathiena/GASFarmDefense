using UnityEngine;
using FD.Data;

namespace FD.Services
{
    /// <summary>
    /// Implementation cho path-based movement
    /// Logic từ FDEnemyBase.MoveAlongPath() đã được refactor
    /// </summary>
    public class PathMovementService : IEnemyMovementService
    {
        public Vector3 CalculateNextPosition(EnemyState state, EnemyData config, float deltaTime)
        {
            // Validation
            if (state.PathPoints == null || state.PathPoints.Length == 0)
                return state.CurrentPosition;
            
            if (state.CurrentPathIndex >= state.PathPoints.Length)
                return state.CurrentPosition;
            
            var targetWaypoint = state.PathPoints[state.CurrentPathIndex];
            if (targetWaypoint == null)
                return state.CurrentPosition;
            
            // Calculate movement
            Vector3 direction = CalculateDirection(state.CurrentPosition, targetWaypoint.position);
            float moveDistance = config.MoveSpeed * deltaTime;
            
            return state.CurrentPosition + direction * moveDistance;
        }
        
        public bool HasReachedWaypoint(Vector3 currentPos, Vector3 targetPos, float threshold)
        {
            return Vector3.Distance(currentPos, targetPos) <= threshold;
        }
        
        public Vector3 CalculateDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            return direction.normalized;
        }
        
        public int GetNextPathIndex(EnemyState state, Vector3 currentPos, float threshold)
        {
            if (state.PathPoints == null || state.CurrentPathIndex >= state.PathPoints.Length)
                return state.CurrentPathIndex;
            
            var currentWaypoint = state.PathPoints[state.CurrentPathIndex];
            if (currentWaypoint == null)
                return state.CurrentPathIndex + 1;
            
            if (HasReachedWaypoint(currentPos, currentWaypoint.position, threshold))
                return state.CurrentPathIndex + 1;
            
            return state.CurrentPathIndex;
        }
    }
}
