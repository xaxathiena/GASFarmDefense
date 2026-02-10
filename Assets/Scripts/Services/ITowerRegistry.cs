using UnityEngine;
using System.Collections.Generic;

namespace FD.Services
{
    /// <summary>
    /// Interface cho Tower registry - theo dõi all towers
    /// </summary>
    public interface ITowerRegistry
    {
        void Register(ITower tower);
        void Unregister(ITower tower);
        List<ITower> GetAllTowers();
        List<ITower> GetTowersInRange(Vector3 position, float range);
        int TowerCount { get; }
    }
    
    /// <summary>
    /// Interface cho Tower entity
    /// </summary>
    public interface ITower
    {
        string TowerID { get; }
        Vector3 Position { get; }
        bool IsActive { get; }
        float TargetRange { get; }
    }
}
