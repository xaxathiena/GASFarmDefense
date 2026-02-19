using UnityEngine;
using VContainer;
using Abel.TowerDefense.Render;

namespace Abel.TowerDefense.InputSystem
{
    public class InputCreateUnit : MonoBehaviour
    {
        // Inject the manager
        [SerializeField] private string unitIDToSpawn = "Goblin_Archer"; // Set this in Inspector to choose which unit to spawn
        [SerializeField] private Transform[] spawnPoints; // Optional: predefined spawn points, or we can just use mouse position
        private GameUnitManager unitManager;
        [Inject]
        public void Construct(GameUnitManager manager)
        {
            unitManager = manager;
        }
        void Start()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("No spawn points assigned. Units will spawn at mouse position.");
            }
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        // Renderer maps position.x→World X, position.y→World Z (XZ plane).
                        // Must use (x, z) — NOT (x, y) — otherwise all units land on Y=0 → horizontal line.
                        unitManager.SpawnUnit(unitIDToSpawn, new Vector2(point.position.x, point.position.z));
                    }
                    point.gameObject.SetActive(false);
                }
            }
        }
        void Update()
        {
           // unitManager.SpawnUnit(unitIDToSpawn, GetMouseWorldPosition());
        }

        private Vector2 GetMouseWorldPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // XZ Plane at Y=0

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                return new Vector2(hitPoint.x, hitPoint.z);
            }
            return Vector2.zero; // Fallback
        }
    }
}