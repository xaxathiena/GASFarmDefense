using System.Collections.Generic;
using UnityEngine;

namespace FD.Services
{
    /// <summary>
    /// Service quản lý danh sách enemies (thay thế EnemyManager singleton)
    /// </summary>
    public interface IEnemyRegistry
    {
        // Registration
        void Register(IEnemy enemy);
        void Unregister(IEnemy enemy);
        
        // Queries
        IReadOnlyList<IEnemy> GetAllEnemies();
        IReadOnlyList<IEnemy> GetEnemiesInRange(Vector3 position, float range, int layerMask);
        IEnemy GetNearestEnemy(Vector3 position);
        
        // Stats
        int ActiveCount { get; }
        
        // Cleanup
        void ClearAll();
    }
    
    /// <summary>
    /// Interface cho enemy objects
    /// </summary>
    public interface IEnemy
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        GameObject GameObject { get; }
        int Layer { get; }
        bool IsActive { get; }
        bool IsAlive { get; }
    }
}
