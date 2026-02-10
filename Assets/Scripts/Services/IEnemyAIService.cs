using FD.Data;

namespace FD.Services
{
    /// <summary>
    /// AI decision making service
    /// </summary>
    public interface IEnemyAIService
    {
        /// <summary>
        /// Quyết định action tiếp theo dựa trên state và config
        /// </summary>
        EnemyAIDecision Decide(EnemyState state, EnemyData config);
    }
    
    public enum EnemyAIDecision
    {
        Idle,           // Không làm gì
        FollowPath,     // Đi theo path
        MoveToTarget,   // Đuổi theo target
        Attack,         // Tấn công
        Flee            // Chạy trốn (future)
    }
}
