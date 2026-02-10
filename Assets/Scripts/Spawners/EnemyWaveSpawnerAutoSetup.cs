using UnityEngine;
using FD.Data;

namespace FD.Spawners
{
    /// <summary>
    /// Helper script để auto-assign references cho EnemyWaveSpawner trong test scene
    /// </summary>
    [RequireComponent(typeof(EnemyWaveSpawner))]
    public class EnemyWaveSpawnerAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup Settings")]
        [SerializeField] private bool autoSetupOnStart = true;
        
        [Header("Reference Names (Optional)")]
        [SerializeField] private string spawnPointName = "SpawnPoint";
        [SerializeField] private string waypoint1Name = "Waypoint1";
        [SerializeField] private string waypoint2Name = "Waypoint2";
        
        [Header("Asset Paths (Optional)")]
        [SerializeField] private string enemyPrefabPath = "Assets/_Master/VContainer/Prefabs/EnemyPrefab.prefab";
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
            var spawner = GetComponent<EnemyWaveSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[EnemyWaveSpawnerAutoSetup] No EnemyWaveSpawner component found!");
                return;
            }
            
            // Find references in scene
            var spawnPoint = GameObject.Find(spawnPointName);
            var waypoint1 = GameObject.Find(waypoint1Name);
            var waypoint2 = GameObject.Find(waypoint2Name);
            
            if (spawnPoint == null)
            {
                Debug.LogWarning($"[EnemyWaveSpawnerAutoSetup] {spawnPointName} not found in scene!");
            }
            
            if (waypoint1 == null || waypoint2 == null)
            {
                Debug.LogWarning("[EnemyWaveSpawnerAutoSetup] Waypoints not found in scene!");
            }
            
            // Load assets
            GameObject enemyPrefab = null;
            EnemyConfigSO config = null;
            
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(enemyPrefabPath))
            {
                enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
                if (enemyPrefab == null)
                {
                    Debug.LogWarning($"[EnemyWaveSpawnerAutoSetup] Could not load prefab at: {enemyPrefabPath}");
                }
            }
            
            if (!string.IsNullOrEmpty(enemyConfigPath))
            {
                config = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyConfigSO>(enemyConfigPath);
                if (config == null)
                {
                    Debug.LogWarning($"[EnemyWaveSpawnerAutoSetup] Could not load config at: {enemyConfigPath}");
                }
            }
#endif
            
            // Use reflection to set private serialized fields
            var spawnerType = spawner.GetType();
            
            if (spawnPoint != null)
            {
                var spawnPointField = spawnerType.GetField("spawnPoint", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnPointField != null)
                {
                    spawnPointField.SetValue(spawner, spawnPoint.transform);
                    Debug.Log("[EnemyWaveSpawnerAutoSetup] Set spawnPoint");
                }
            }
            
            if (waypoint1 != null && waypoint2 != null)
            {
                var pathPointsField = spawnerType.GetField("pathPoints", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pathPointsField != null)
                {
                    pathPointsField.SetValue(spawner, new Transform[] { waypoint1.transform, waypoint2.transform });
                    Debug.Log("[EnemyWaveSpawnerAutoSetup] Set pathPoints");
                }
            }
            
            // Setup a default wave if assets are loaded
            if (enemyPrefab != null && config != null)
            {
                var wavesField = spawnerType.GetField("waves", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (wavesField != null)
                {
                    var waves = new System.Collections.Generic.List<EnemyWave>();
                    var wave = new EnemyWave
                    {
                        waveName = "Test Wave",
                        delayBeforeWave = 0f,
                        enemies = new System.Collections.Generic.List<EnemyWaveEntry>
                        {
                            new EnemyWaveEntry
                            {
                                enemyViewPrefab = enemyPrefab,
                                config = config,
                                count = 3,
                                spawnInterval = 1f
                            }
                        }
                    };
                    waves.Add(wave);
                    wavesField.SetValue(spawner, waves);
                    Debug.Log("[EnemyWaveSpawnerAutoSetup] Set default test wave (3 enemies)");
                }
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(spawner);
#endif
            
            Debug.Log("[EnemyWaveSpawnerAutoSetup] ✅ Setup completed!");
        }
    }
}
