using UnityEngine;
using VContainer;
using Abel.TowerDefense.Render;

namespace Abel.TowerDefense.InputSystem
{
    public class InputCreateUnit : MonoBehaviour
    {
        // Inject the manager
        [SerializeField] private string unitIDToSpawn = "Goblin_Archer"; // Set this in Inspector to choose which unit to spawn
        [SerializeField] private string enemyIDToSpawn = "";
        [SerializeField] private Transform[] spawnPoints; // Optional: predefined spawn points, or we can just use mouse position
        [SerializeField] private Transform[] pathTransforms; // Optional: predefined path points, or we can set them dynamically later

        [Header("Spawn Control")]
        [SerializeField] private float spawnInterval = 1.0f; // Time between spawns when using timer-based spawning
        private float spawnTimer = 0f;
        private Vector2[] cachedPath;
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
            if (pathTransforms != null && pathTransforms.Length > 0)
            {
                cachedPath = new Vector2[pathTransforms.Length];
                for (int i = 0; i < pathTransforms.Length; i++)
                {
                    cachedPath[i] = new Vector2(pathTransforms[i].position.x, pathTransforms[i].position.z);
                    pathTransforms[i].gameObject.SetActive(false); // Ẩn cột mốc đi
                }
            }
        }
        void Update()
        {
            if (cachedPath == null || cachedPath.Length == 0) return;

            // Bộ đếm thời gian Spawn tự động
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                spawnTimer = spawnInterval;

                // Spawn ngay tại điểm đầu tiên của Path
                unitManager.SpawnUnit(enemyIDToSpawn, cachedPath[0]);
            }
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