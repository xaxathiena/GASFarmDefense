using UnityEngine;
using VContainer;
using FD.Data;
using FD.Views;
using FD.Controllers;
using FD.DI;

namespace FD.Spawners
{
    /// <summary>
    /// Spawns enemies sử dụng factory pattern với VContainer injection
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject enemyViewPrefab;
        
        [Header("Config")]
        [SerializeField] private EnemyConfigSO defaultConfig;
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform[] pathPoints;
        
        [Header("Debug")]
        [SerializeField] private bool logSpawns = true;
        
        // Factory injected by VContainer
        private EnemyControllerFactory _enemyFactory;
        
        // Track spawned controllers để update manually
        // NOTE: Controllers implement ITickable sẽ được VContainer tự động tick
        // nhưng vì controllers được tạo runtime, chúng ta cần register manually
        private System.Collections.Generic.List<EnemyController> _activeControllers = 
            new System.Collections.Generic.List<EnemyController>();
        
        [Inject]
        public void Construct(EnemyControllerFactory enemyFactory)
        {
            _enemyFactory = enemyFactory;
            
            if (logSpawns)
                Debug.Log("[EnemySpawner] Constructed with VContainer injection!");
        }
        
        private void Update()
        {
            // Manual tick cho controllers
            // TODO: Có thể optimize bằng cách dùng VContainer's ITickable registration
            for (int i = _activeControllers.Count - 1; i >= 0; i--)
            {
                var controller = _activeControllers[i];
                if (controller == null || !controller.IsAlive)
                {
                    _activeControllers.RemoveAt(i);
                    continue;
                }
                
                controller.Tick();
            }
        }
        
        public EnemyController SpawnEnemy()
        {
            if (defaultConfig == null)
            {
                Debug.LogError("[EnemySpawner] Default config is null!");
                return null;
            }
            
            return SpawnEnemy(defaultConfig.ToEnemyData());
        }
        
        public EnemyController SpawnEnemy(EnemyData config)
        {
            // Validate factory
            if (_enemyFactory == null)
            {
                Debug.LogError("[EnemySpawner] Enemy factory is null! VContainer injection failed?");
                return null;
            }
            
            // Validate
            if (enemyViewPrefab == null)
            {
                Debug.LogError("[EnemySpawner] Enemy view prefab is null!");
                return null;
            }
            
            // Instantiate view
            Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            var viewGO = Instantiate(enemyViewPrefab, position, Quaternion.identity);
            var view = viewGO.GetComponent<EnemyView>();
            
            if (view == null)
            {
                Debug.LogError("[EnemySpawner] Prefab missing EnemyView component!");
                Destroy(viewGO);
                return null;
            }
            
            // Create controller với VContainer factory!
            var controller = _enemyFactory.Create(view, config);
            
            // Setup initial state
            if (pathPoints != null && pathPoints.Length > 0)
            {
                controller.SetPath(pathPoints);
            }
            
            // Track controller
            _activeControllers.Add(controller);
            
            if (logSpawns)
            {
                Debug.Log($"[EnemySpawner] Spawned enemy '{config.EnemyID}' at {position}");
            }
            
            return controller;
        }
        
        // Example: Spawn wave
        public void SpawnWave(int count, float interval)
        {
            StartCoroutine(SpawnWaveCoroutine(count, interval));
        }
        
        private System.Collections.IEnumerator SpawnWaveCoroutine(int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(interval);
            }
        }
        
        // Test button
        [ContextMenu("Spawn Test Enemy")]
        private void SpawnTestEnemy()
        {
            SpawnEnemy();
        }
        
        [ContextMenu("Spawn Test Wave (5 enemies)")]
        private void SpawnTestWave()
        {
            SpawnWave(5, 1f);
        }
        
        private void OnDestroy()
        {
            // Cleanup controllers
            foreach (var controller in _activeControllers)
            {
                controller?.Dispose();
            }
            _activeControllers.Clear();
        }
    }
}
