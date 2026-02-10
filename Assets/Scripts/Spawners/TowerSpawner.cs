using UnityEngine;
using VContainer;
using System.Collections.Generic;
using FD.Data;
using FD.Views;
using FD.Services;

namespace FD.Spawners
{
    /// <summary>
    /// Tower spawner với VContainer
    /// Tương tự PerformanceTestManager nhưng dùng VContainer architecture
    /// </summary>
    public class TowerSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private List<GameObject> towerViewPrefabs = new List<GameObject>();
        
        [Header("Configs")]
        [SerializeField] private List<TowerConfigSO> towerConfigs = new List<TowerConfigSO>();
        [SerializeField] private bool randomizeTowerTypes = true;
        
        [Header("Spawn Settings")]
        [SerializeField] private int numberOfTowers = 10;
        [SerializeField] private bool spawnTowersOnStart = false;
        
        [Header("Tower Placement")]
        [SerializeField] private Transform[] towerSpawnPoints;
        [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(20f, 0f, 10f);
        
        [Header("Path Reference (for placement near path)")]
        [SerializeField] private Transform[] pathPoints;
        [SerializeField] private float offsetFromPath = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool logSpawns = true;
        
        // Injected services (VContainer sẽ tự động inject khi Tower system được add vào GameLifetimeScope)
        private ITowerRegistry _towerRegistry;
        
        // Track spawned towers
        private List<GameObject> _spawnedTowerViews = new List<GameObject>();
        
        // NOTE: Tower không có controller pattern như Enemy vì TowerBase đã có sẵn logic
        // Chỉ cần spawn prefab và registry sẽ track
        
        [Inject]
        public void Construct(ITowerRegistry towerRegistry)
        {
            _towerRegistry = towerRegistry;
            
            if (logSpawns)
                Debug.Log("[TowerSpawner] Constructed with VContainer injection!");
        }
        
        private void Start()
        {
            if (spawnTowersOnStart)
            {
                SpawnTowers();
            }
        }
        
        [ContextMenu("Spawn Towers")]
        public void SpawnTowers()
        {
            ClearTowers();
            
            if (towerViewPrefabs.Count == 0 || towerConfigs.Count == 0)
            {
                Debug.LogWarning("[TowerSpawner] No tower prefabs or configs assigned!");
                return;
            }
            
            // Use spawn points if available
            if (towerSpawnPoints != null && towerSpawnPoints.Length > 0)
            {
                SpawnTowersAtPoints();
            }
            // Otherwise spawn near path
            else if (pathPoints != null && pathPoints.Length > 0)
            {
                SpawnTowersNearPath();
            }
            // Fallback: spawn in area
            else
            {
                SpawnTowersInArea();
            }
            
            if (logSpawns)
                Debug.Log($"[TowerSpawner] Spawned {_spawnedTowerViews.Count} towers");
        }
        
        [ContextMenu("Clear Towers")]
        public void ClearTowers()
        {
            foreach (var towerView in _spawnedTowerViews)
            {
                if (towerView != null)
                {
                    Destroy(towerView);
                }
            }
            _spawnedTowerViews.Clear();
        }
        
        private void SpawnTowersAtPoints()
        {
            int towersToSpawn = Mathf.Min(numberOfTowers, towerSpawnPoints.Length);
            
            for (int i = 0; i < towersToSpawn; i++)
            {
                if (towerSpawnPoints[i] == null) continue;
                
                SpawnTower(towerSpawnPoints[i].position, i);
            }
        }
        
        private void SpawnTowersNearPath()
        {
            if (pathPoints.Length < 2) return;
            
            for (int i = 0; i < numberOfTowers; i++)
            {
                // Get a point along the path
                int pathIndex = Mathf.FloorToInt((float)i / numberOfTowers * (pathPoints.Length - 1));
                pathIndex = Mathf.Clamp(pathIndex, 0, pathPoints.Length - 1);
                
                Vector3 pathPosition = pathPoints[pathIndex].position;
                
                // Offset perpendicular to path
                Vector3 offset = Vector3.zero;
                if (pathIndex < pathPoints.Length - 1)
                {
                    Vector3 pathDirection = (pathPoints[pathIndex + 1].position - pathPosition).normalized;
                    Vector3 perpendicular = new Vector3(-pathDirection.z, 0, pathDirection.x);
                    offset = perpendicular * offsetFromPath * (i % 2 == 0 ? 1f : -1f);
                }
                else
                {
                    offset = new Vector3(offsetFromPath * (i % 2 == 0 ? 1f : -1f), 0, 0);
                }
                
                Vector3 spawnPosition = pathPosition + offset;
                SpawnTower(spawnPosition, i);
            }
        }
        
        private void SpawnTowersInArea()
        {
            for (int i = 0; i < numberOfTowers; i++)
            {
                float x = spawnAreaCenter.x + Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
                float z = spawnAreaCenter.z + Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2);
                Vector3 spawnPosition = new Vector3(x, spawnAreaCenter.y, z);
                
                SpawnTower(spawnPosition, i);
            }
        }
        
        private void SpawnTower(Vector3 position, int index)
        {
            // Select prefab and config
            GameObject prefab = randomizeTowerTypes
                ? towerViewPrefabs[Random.Range(0, towerViewPrefabs.Count)]
                : towerViewPrefabs[index % towerViewPrefabs.Count];
                
            TowerConfigSO config = randomizeTowerTypes
                ? towerConfigs[Random.Range(0, towerConfigs.Count)]
                : towerConfigs[index % towerConfigs.Count];
            
            // Instantiate tower
            var towerGO = Instantiate(prefab, position, Quaternion.identity, transform);
            _spawnedTowerViews.Add(towerGO);
            
            // NOTE: Hiện tại chưa có TowerController pattern
            // TowerBase cũ vẫn hoạt động bình thường
            // Khi migrate TowerBase sang architecture mới, sẽ tạo TowerController ở đây
            
            if (logSpawns)
                Debug.Log($"[TowerSpawner] Spawned tower at {position}");
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw spawn area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
            
            // Draw tower spawn points
            if (towerSpawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in towerSpawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                    }
                }
            }
            
            // Draw path with offset visualization
            if (pathPoints != null && pathPoints.Length > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < pathPoints.Length - 1; i++)
                {
                    if (pathPoints[i] != null && pathPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                        
                        // Draw perpendicular offset lines
                        Vector3 direction = (pathPoints[i + 1].position - pathPoints[i].position).normalized;
                        Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);
                        Vector3 midPoint = (pathPoints[i].position + pathPoints[i + 1].position) * 0.5f;
                        
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(midPoint, midPoint + perpendicular * offsetFromPath);
                        Gizmos.DrawLine(midPoint, midPoint - perpendicular * offsetFromPath);
                    }
                }
            }
        }
    }
}
