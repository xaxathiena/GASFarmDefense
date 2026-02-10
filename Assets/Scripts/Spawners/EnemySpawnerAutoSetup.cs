using UnityEngine;
using FD.Data;
using FD.Views;

namespace FD.Spawners
{
    /// <summary>
    /// Helper script để auto-assign references cho EnemySpawner trong test scene
    /// </summary>
    [RequireComponent(typeof(EnemySpawner))]
    public class EnemySpawnerAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup Settings")]
        [SerializeField] private bool autoSetupOnStart = true;
        
        [Header("Prefab Path (Optional)")]
        [SerializeField] private string enemyPrefabPath = "Assets/_Master/VContainer/Prefabs/EnemyPrefab.prefab";
        
        [Header("Config Path (Optional)")]
        [SerializeField] private string enemyConfigPath = "Assets/_Master/VContainer/Configs/BasicEnemyConfig.asset";
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupReferences();
            }
        }
        
        [ContextMenu("Setup References")]
        public void SetupReferences()
        {
            var spawner = GetComponent<EnemySpawner>();
            if (spawner == null)
            {
                Debug.LogError("[EnemySpawnerAutoSetup] No EnemySpawner component found!");
                return;
            }
            
            // Find references in scene
            var spawnPoint = GameObject.Find("SpawnPoint");
            var waypoint1 = GameObject.Find("Waypoint1");
            var waypoint2 = GameObject.Find("Waypoint2");
            
            if (spawnPoint == null)
            {
                Debug.LogWarning("[EnemySpawnerAutoSetup] SpawnPoint not found in scene!");
            }
            
            if (waypoint1 == null || waypoint2 == null)
            {
                Debug.LogWarning("[EnemySpawnerAutoSetup] Waypoints not found in scene!");
            }
            
            // Load prefab from Resources if path provided
            GameObject prefab = null;
            EnemyConfigSO config = null;
            
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(enemyPrefabPath))
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[EnemySpawnerAutoSetup] Could not load prefab at: {enemyPrefabPath}");
                }
            }
            
            if (!string.IsNullOrEmpty(enemyConfigPath))
            {
                config = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyConfigSO>(enemyConfigPath);
                if (config == null)
                {
                    Debug.LogWarning($"[EnemySpawnerAutoSetup] Could not load config at: {enemyConfigPath}");
                }
            }
#endif
            
            // Use reflection to set private serialized fields
            var spawnerType = spawner.GetType();
            
            if (prefab != null)
            {
                var prefabField = spawnerType.GetField("enemyViewPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prefabField != null)
                {
                    prefabField.SetValue(spawner, prefab);
                    Debug.Log("[EnemySpawnerAutoSetup] Set enemyViewPrefab");
                }
            }
            
            if (config != null)
            {
                var configField = spawnerType.GetField("defaultConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (configField != null)
                {
                    configField.SetValue(spawner, config);
                    Debug.Log("[EnemySpawnerAutoSetup] Set defaultConfig");
                }
            }
            
            if (spawnPoint != null)
            {
                var spawnPointField = spawnerType.GetField("spawnPoint", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnPointField != null)
                {
                    spawnPointField.SetValue(spawner, spawnPoint.transform);
                    Debug.Log("[EnemySpawnerAutoSetup] Set spawnPoint");
                }
            }
            
            if (waypoint1 != null && waypoint2 != null)
            {
                var pathPointsField = spawnerType.GetField("pathPoints", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pathPointsField != null)
                {
                    pathPointsField.SetValue(spawner, new Transform[] { waypoint1.transform, waypoint2.transform });
                    Debug.Log("[EnemySpawnerAutoSetup] Set pathPoints");
                }
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(spawner);
#endif
            
            Debug.Log("[EnemySpawnerAutoSetup] ✅ Setup completed!");
        }
    }
}
