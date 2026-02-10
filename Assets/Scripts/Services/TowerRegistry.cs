using UnityEngine;
using System.Collections.Generic;

namespace FD.Services
{
    /// <summary>
    /// Tower registry implementation - global tracking
    /// Singleton service, stateless queries
    /// </summary>
    public class TowerRegistry : ITowerRegistry
    {
        private readonly List<ITower> _towers = new List<ITower>();
        
        public int TowerCount => _towers.Count;
        
        public void Register(ITower tower)
        {
            if (tower == null || _towers.Contains(tower))
                return;
                
            _towers.Add(tower);
        }
        
        public void Unregister(ITower tower)
        {
            if (tower == null)
                return;
                
            _towers.Remove(tower);
        }
        
        public List<ITower> GetAllTowers()
        {
            return new List<ITower>(_towers);
        }
        
        public List<ITower> GetTowersInRange(Vector3 position, float range)
        {
            var result = new List<ITower>();
            float sqrRange = range * range;
            
            foreach (var tower in _towers)
            {
                if (tower == null || !tower.IsActive)
                    continue;
                    
                float sqrDistance = (tower.Position - position).sqrMagnitude;
                if (sqrDistance <= sqrRange)
                {
                    result.Add(tower);
                }
            }
            
            return result;
        }
    }
}
