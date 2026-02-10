using System;
using UnityEngine;
using FD.Services;

namespace FD.Events
{
    // Base class cho enemy events
    public abstract class EnemyEventBase : IGameplayEvent
    {
        public IEnemy Enemy { get; }
        public float Timestamp { get; }
        
        protected EnemyEventBase(IEnemy enemy)
        {
            Enemy = enemy;
            Timestamp = Time.time;
        }
    }
    
    // Spawning/Despawning
    public class EnemySpawnedEvent : EnemyEventBase
    {
        public EnemySpawnedEvent(IEnemy enemy) : base(enemy) { }
    }
    
    public class EnemyDespawnedEvent : EnemyEventBase
    {
        public EnemyDespawnedEvent(IEnemy enemy) : base(enemy) { }
    }
    
    // Movement
    public class EnemyReachedWaypointEvent : EnemyEventBase
    {
        public int WaypointIndex { get; }
        
        public EnemyReachedWaypointEvent(IEnemy enemy, int waypointIndex) : base(enemy)
        {
            WaypointIndex = waypointIndex;
        }
    }
    
    public class EnemyReachedPathEndEvent : EnemyEventBase
    {
        public EnemyReachedPathEndEvent(IEnemy enemy) : base(enemy) { }
    }
    
    // Combat
    public class EnemyAttackEvent : EnemyEventBase
    {
        public Transform Target { get; }
        
        public EnemyAttackEvent(IEnemy enemy, Transform target) : base(enemy)
        {
            Target = target;
        }
    }
    
    public class EnemyDamagedEvent : EnemyEventBase
    {
        public float DamageAmount { get; }
        
        public EnemyDamagedEvent(IEnemy enemy, float damageAmount) : base(enemy)
        {
            DamageAmount = damageAmount;
        }
    }
    
    public class EnemyDiedEvent : EnemyEventBase
    {
        public EnemyDiedEvent(IEnemy enemy) : base(enemy) { }
    }
}
