using UnityEngine;

namespace FD.Views
{
    /// <summary>
    /// Minimal MonoBehaviour view layer cho Tower
    /// Không chứa game logic, chỉ visual và lifecycle
    /// </summary>
    public class TowerView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Transform towerVisual;
        [SerializeField] private Transform weaponPoint;
        
        // Lifecycle events
        public event System.Action<TowerView> OnSpawned;
        public event System.Action<TowerView> OnDespawned;
        
        public Transform WeaponPoint => weaponPoint != null ? weaponPoint : transform;
        public Transform Visual => towerVisual != null ? towerVisual : transform;
        
        private void OnEnable()
        {
            OnSpawned?.Invoke(this);
        }
        
        private void OnDisable()
        {
            OnDespawned?.Invoke(this);
        }
        
        /// <summary>
        /// Update visual direction (rotate towards target)
        /// </summary>
        public void UpdateRotation(Vector3 targetPosition)
        {
            if (towerVisual == null)
                return;
                
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0; // Keep upright
            
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                towerVisual.rotation = Quaternion.Slerp(towerVisual.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Weapon point visualization
            if (weaponPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(weaponPoint.position, 0.2f);
            }
        }
    }
}
