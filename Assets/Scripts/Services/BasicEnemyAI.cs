using UnityEngine;
using FD.Data;

namespace FD.Services
{
    /// <summary>
    /// Simple AI implementation
    /// Logic từ EnemyBase.UpdateBehavior() đã được refactor
    /// </summary>
    public class BasicEnemyAI : IEnemyAIService
    {
        public EnemyAIDecision Decide(EnemyState state, EnemyData config)
        {
            // Dead or inactive
            if (!state.IsAlive || !state.IsActive)
                return EnemyAIDecision.Idle;
            
            // Path following có priority cao nhất
            if (state.PathPoints != null && state.PathPoints.Length > 0)
            {
                if (!state.HasReachedPathEnd)
                    return EnemyAIDecision.FollowPath;
            }
            
            // Behavior based on target
            if (state.HasValidTarget())
            {
                float distance = Vector3.Distance(state.CurrentPosition, state.CurrentTarget.position);
                
                // In attack range
                if (distance <= config.AttackRange)
                {
                    if (state.CanAttack(Time.time, config.AttackCooldown))
                        return EnemyAIDecision.Attack;
                    else
                        return EnemyAIDecision.Idle; // Cooldown
                }
                
                // In detection range
                if (distance <= config.DetectionRange)
                    return EnemyAIDecision.MoveToTarget;
            }
            
            return EnemyAIDecision.Idle;
        }
    }
}
