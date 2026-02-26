using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Minimal contract for anything that can instantiate a tower unit at a world position.
    /// Keeping this separate from ITowerManager lets the drag-drop system depend only on
    /// what it actually needs (single-responsibility / interface-segregation principle).
    /// </summary>
    public interface ITowerSpawner
    {
        /// <summary>
        /// Spawns a tower of the given <paramref name="unitID"/> at <paramref name="position"/>.
        /// Implementations handle GameObject instantiation and DOD data registration.
        /// </summary>
        void SpawnTower(string unitID, Vector3 position);
    }
}
