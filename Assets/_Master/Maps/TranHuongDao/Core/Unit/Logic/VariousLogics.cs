using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Dummy logic for pet followers - would move towards owner.
    /// Implemented as a placeholder for extensibility.
    /// </summary>
    public class PetFollowerLogic : IUnitLogic
    {
        public void OnEnter(Minion minion) { }

        public void Tick(Minion minion, float dt)
        {
            // Placeholder: follow owner or stays nearby.
        }

        public void OnExit(Minion minion) { }
    }

    /// <summary>
    /// Logic for units following a path (Standard Enemy logic).
    /// </summary>
    public class PathFollowerLogic : IUnitLogic
    {
        public void OnEnter(Minion minion) { }

        public void Tick(Minion minion, float dt)
        {
            // Note: For minions following a path, they would need a Path reference.
            // In a unified system, Enemy.cs would be replaced by Minion + PathFollowerLogic.
        }

        public void OnExit(Minion minion) { }
    }
}
