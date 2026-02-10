using UnityEngine;

namespace FD.Data
{
    /// <summary>
    /// Runtime state của enemy - Thay đổi trong gameplay
    /// Tách biệt với config để dễ serialize/reset
    /// </summary>
    public class EnemyState
    {
        // Position & Movement
        public Vector3 CurrentPosition { get; set; }
        public Vector3 CurrentVelocity { get; set; }
        
        // Pathfinding
        public Transform CurrentTarget { get; set; }
        public Transform[] PathPoints { get; set; }
        public int CurrentPathIndex { get; set; }
        public bool HasReachedPathEnd { get; set; }
        
        // Combat
        public float LastAttackTime { get; set; }
        public bool IsAttacking { get; set; }
        
        // Status
        public bool IsAlive { get; set; } = true;
        public bool IsActive { get; set; } = true;
        
        // Reset method để reuse state object
        public void Reset()
        {
            CurrentPathIndex = 0;
            HasReachedPathEnd = false;
            IsAttacking = false;
            LastAttackTime = 0f;
            IsAlive = true;
            IsActive = true;
        }
        
        // Query helpers (không có side effects)
        public bool CanAttack(float currentTime, float cooldown)
        {
            return IsAlive && !IsAttacking && (currentTime - LastAttackTime) >= cooldown;
        }
        
        public bool HasValidTarget()
        {
            return CurrentTarget != null && CurrentTarget.gameObject.activeInHierarchy;
        }
    }
}
