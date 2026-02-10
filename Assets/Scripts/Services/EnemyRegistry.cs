using System.Collections.Generic;
using UnityEngine;

namespace FD.Services
{
    /// <summary>
    /// Thay thế EnemyManager singleton
    /// ✅ Testable - không cần MonoBehaviour
    /// ✅ Injectable - VContainer quản lý lifetime
    /// ✅ Pure C# - không Unity dependencies
    /// </summary>
    public class EnemyRegistry : IEnemyRegistry
    {
        private readonly List<IEnemy> _enemies = new List<IEnemy>(100);
        private readonly List<IEnemy> _queryBuffer = new List<IEnemy>(50);
        
        public int ActiveCount => _enemies.Count;
        
        public void Register(IEnemy enemy)
        {
            if (enemy == null || _enemies.Contains(enemy))
                return;
            
            _enemies.Add(enemy);
        }
        
        public void Unregister(IEnemy enemy)
        {
            if (enemy == null)
                return;
            
            _enemies.Remove(enemy);
        }
        
        public IReadOnlyList<IEnemy> GetAllEnemies()
        {
            // Cleanup null/inactive
            _enemies.RemoveAll(e => e == null || !e.IsActive);
            return _enemies;
        }
        
        public IReadOnlyList<IEnemy> GetEnemiesInRange(Vector3 position, float range, int layerMask)
        {
            _queryBuffer.Clear();
            
            if (range <= 0f)
                return _queryBuffer;
            
            float rangeSqr = range * range;
            
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                
                // Cleanup
                if (enemy == null || !enemy.IsActive)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }
                
                // Layer check
                if ((layerMask & (1 << enemy.Layer)) == 0)
                    continue;
                
                // Distance check (squared for performance)
                float distSqr = (enemy.Position - position).sqrMagnitude;
                if (distSqr <= rangeSqr)
                    _queryBuffer.Add(enemy);
            }
            
            return _queryBuffer;
        }
        
        public IEnemy GetNearestEnemy(Vector3 position)
        {
            IEnemy nearest = null;
            float minDistSqr = float.MaxValue;
            
            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.IsActive || !enemy.IsAlive)
                    continue;
                
                float distSqr = (enemy.Position - position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = enemy;
                }
            }
            
            return nearest;
        }
        
        public void ClearAll()
        {
            _enemies.Clear();
            _queryBuffer.Clear();
        }
    }
}
