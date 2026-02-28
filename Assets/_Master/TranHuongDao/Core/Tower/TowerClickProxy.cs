using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    // ─────────────────────────────────────────────────────────────────────────────
    // TowerClickProxy
    //
    // Lightweight marker added to a Tower's proxy GameObject at spawn time.
    // It bridges the physics world (Collider) to the logic world (Tower.InstanceID).
    //
    // Usage:
    //   1. TowerManager adds a Collider + this component to Tower.ProxyTransform after Initialize().
    //   2. TowerSelectionManager raycasts, finds this component on the hit object,
    //      reads InstanceID, and resolves the Tower via ITowerManager.TryGetTower().
    // ─────────────────────────────────────────────────────────────────────────────
    public class TowerClickProxy : MonoBehaviour
    {
        /// <summary>
        /// The Tower.InstanceID this proxy represents.
        /// Set once by TowerManager immediately after adding the component.
        /// </summary>
        public int InstanceID { get; set; }
    }
}
