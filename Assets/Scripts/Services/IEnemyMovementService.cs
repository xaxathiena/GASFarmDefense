using UnityEngine;
using FD.Data;

namespace FD.Services
{
    /// <summary>
    /// Service tính toán movement - Pure functions!
    /// ✅ Stateless - dễ test
    /// ✅ Không Unity dependencies - có thể burst compile
    /// </summary>
    public interface IEnemyMovementService
    {
        /// <summary>
        /// Tính vị trí tiếp theo dựa trên state và config
        /// </summary>
        Vector3 CalculateNextPosition(EnemyState state, EnemyData config, float deltaTime);
        
        /// <summary>
        /// Kiểm tra đã đến waypoint chưa
        /// </summary>
        bool HasReachedWaypoint(Vector3 currentPos, Vector3 targetPos, float threshold);
        
        /// <summary>
        /// Tính direction vector từ current đến target
        /// </summary>
        Vector3 CalculateDirection(Vector3 from, Vector3 to);
        
        /// <summary>
        /// Advance path index nếu cần
        /// </summary>
        int GetNextPathIndex(EnemyState state, Vector3 currentPos, float threshold);
    }
}
