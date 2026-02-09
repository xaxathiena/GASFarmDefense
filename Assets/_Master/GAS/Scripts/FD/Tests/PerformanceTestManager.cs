using System.Collections.Generic;
using UnityEngine;
using FD.Character;

namespace FD.Tests
{
    /// <summary>
    /// Manager for performance testing - spawns towers and tracks performance metrics
    /// </summary>
    public class PerformanceTestManager : MonoBehaviour
    {
        [Header("Tower Spawn Settings")]
        [SerializeField] private List<TowerBase> towerPrefabs = new List<TowerBase>();
        [SerializeField] private int numberOfTowers = 10;
        [SerializeField] private bool spawnTowersOnStart = true;
        [SerializeField] private bool randomizeTowerTypes = true;
        
        [Header("Tower Placement")]
        [SerializeField] private Transform[] towerSpawnPoints;
        [SerializeField] private float spacingBetweenTowers = 3f;
        [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(20f, 0f, 10f);
        
        [Header("Path Reference (for placement near path)")]
        [SerializeField] private Transform[] pathPoints;
        [SerializeField] private float offsetFromPath = 2f;
        
        [Header("Performance Metrics")]
        [SerializeField] private bool showPerformanceStats = true;
        [SerializeField] private float updateStatsInterval = 1f;
        
        private List<TowerBase> spawnedTowers = new List<TowerBase>();
        private float nextStatsUpdate;
        private int frameCount;
        private float fps;

        private void Start()
        {
            if (spawnTowersOnStart)
            {
                SpawnTowers();
            }
        }

        private void Update()
        {
            if (showPerformanceStats)
            {
                UpdatePerformanceStats();
            }
        }

        public void SpawnTowers()
        {
            ClearTowers();
            
            if (towerPrefabs.Count == 0)
            {
                Debug.LogWarning("No tower prefabs assigned!");
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
            
#if UNITY_EDITOR
            Debug.Log($"Spawned {spawnedTowers.Count} towers for performance testing");
#endif
        }

        private void SpawnTowersAtPoints()
        {
            int towersToSpawn = Mathf.Min(numberOfTowers, towerSpawnPoints.Length);
            
            for (int i = 0; i < towersToSpawn; i++)
            {
                if (towerSpawnPoints[i] == null) continue;
                
                TowerBase prefab = randomizeTowerTypes 
                    ? towerPrefabs[Random.Range(0, towerPrefabs.Count)]
                    : towerPrefabs[i % towerPrefabs.Count];
                
                TowerBase tower = Instantiate(prefab, towerSpawnPoints[i].position, Quaternion.identity, transform);
                spawnedTowers.Add(tower);
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
                
                TowerBase prefab = randomizeTowerTypes 
                    ? towerPrefabs[Random.Range(0, towerPrefabs.Count)]
                    : towerPrefabs[i % towerPrefabs.Count];
                
                TowerBase tower = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
                spawnedTowers.Add(tower);
            }
        }

        private void SpawnTowersInArea()
        {
            for (int i = 0; i < numberOfTowers; i++)
            {
                float x = spawnAreaCenter.x + Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
                float z = spawnAreaCenter.z + Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2);
                Vector3 spawnPosition = new Vector3(x, spawnAreaCenter.y, z);
                
                TowerBase prefab = randomizeTowerTypes 
                    ? towerPrefabs[Random.Range(0, towerPrefabs.Count)]
                    : towerPrefabs[i % towerPrefabs.Count];
                
                TowerBase tower = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
                spawnedTowers.Add(tower);
            }
        }

        public void ClearTowers()
        {
            foreach (var tower in spawnedTowers)
            {
                if (tower != null)
                {
                    Destroy(tower.gameObject);
                }
            }
            spawnedTowers.Clear();
        }

        private void UpdatePerformanceStats()
        {
            frameCount++;
            
            if (Time.time >= nextStatsUpdate)
            {
                fps = frameCount / updateStatsInterval;
                frameCount = 0;
                nextStatsUpdate = Time.time + updateStatsInterval;
            }
        }

        private void OnGUI()
        {
            if (!showPerformanceStats) return;
            
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;
            
            GUI.Label(new Rect(10, 10, 300, 30), $"FPS: {fps:F1}", style);
            GUI.Label(new Rect(10, 40, 300, 30), $"Towers: {spawnedTowers.Count}", style);
            GUI.Label(new Rect(10, 70, 300, 30), $"Enemies: {FindObjectsOfType<FDEnemyBase>().Length}", style);
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
        }
    }
}
