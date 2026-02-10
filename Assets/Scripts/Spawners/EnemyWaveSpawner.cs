using UnityEngine;
using VContainer;
using System.Collections;
using System.Collections.Generic;
using FD.Data;
using FD.Views;
using FD.Controllers;
using FD.DI;

namespace FD.Spawners
{
    /// <summary>
    /// Enemy wave spawner với VContainer
    /// Tương tự FDEnemyWaveController nhưng dùng EnemyController architecture
    /// </summary>
    public class EnemyWaveSpawner : MonoBehaviour
    {
        [Header("Spawn & Path")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform[] pathPoints;
        
        [Header("Wave Settings")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private float timeBetweenWaves = 2f;
        [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>();
        
        [Header("Debug")]
        [SerializeField] private bool logSpawns = true;
        
        // Factory injected by VContainer
        private EnemyControllerFactory _enemyFactory;
        
        // Track active controllers
        private List<EnemyController> _activeControllers = new List<EnemyController>();
        private Coroutine _waveRoutine;
        
        [Inject]
        public void Construct(EnemyControllerFactory enemyFactory)
        {
            _enemyFactory = enemyFactory;
            
            if (logSpawns)
                Debug.Log("[EnemyWaveSpawner] Constructed with VContainer injection!");
        }
        
        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartAllWaves();
            }
        }
        
        public void StartAllWaves()
        {
            StopWaves();
            _waveRoutine = StartCoroutine(RunAllWavesRoutine());
        }
        
        public void StartWave(int waveIndex)
        {
            StopWaves();
            if (waveIndex >= 0 && waveIndex < waves.Count)
            {
                _waveRoutine = StartCoroutine(RunSingleWaveRoutine(waveIndex));
            }
        }
        
        public void StopWaves()
        {
            if (_waveRoutine != null)
            {
                StopCoroutine(_waveRoutine);
                _waveRoutine = null;
            }
        }
        
        private IEnumerator RunAllWavesRoutine()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                yield return RunWaveRoutine(waves[i]);
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
        
        private IEnumerator RunSingleWaveRoutine(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
                yield break;
                
            yield return RunWaveRoutine(waves[waveIndex]);
        }
        
        private IEnumerator RunWaveRoutine(EnemyWave wave)
        {
            if (wave == null)
                yield break;
                
            if (logSpawns)
                Debug.Log($"[EnemyWaveSpawner] Starting wave: {wave.waveName}");
                
            if (wave.delayBeforeWave > 0f)
            {
                yield return new WaitForSeconds(wave.delayBeforeWave);
            }
            
            foreach (var entry in wave.enemies)
            {
                if (entry == null || entry.enemyViewPrefab == null || entry.config == null)
                    continue;
                    
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnEnemy(entry);
                    
                    if (entry.spawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(entry.spawnInterval);
                    }
                }
            }
        }
        
        private void SpawnEnemy(EnemyWaveEntry entry)
        {
            Vector3 spawnPosition = spawnPoint != null
                ? spawnPoint.position
                : (pathPoints != null && pathPoints.Length > 0 ? pathPoints[0].position : Vector3.zero);
                
            // Instantiate view
            var viewGO = Instantiate(entry.enemyViewPrefab, spawnPosition, Quaternion.identity);
            var view = viewGO.GetComponent<EnemyView>();
            
            if (view == null)
            {
                Debug.LogError($"[EnemyWaveSpawner] No EnemyView component on prefab: {entry.enemyViewPrefab.name}");
                Destroy(viewGO);
                return;
            }
            
            // Create controller với factory
            var enemyData = entry.config.ToEnemyData();
            var controller = _enemyFactory.Create(view, enemyData);
            
            // Set path
            if (pathPoints != null && pathPoints.Length > 0)
            {
                controller.SetPath(pathPoints);
            }
            
            // Track controller
            _activeControllers.Add(controller);
            
            if (logSpawns)
                Debug.Log($"[EnemyWaveSpawner] Spawned enemy at {spawnPosition}");
        }
        
        private void Update()
        {
            // Manual tick cho controllers
            for (int i = _activeControllers.Count - 1; i >= 0; i--)
            {
                if (_activeControllers[i] == null)
                {
                    _activeControllers.RemoveAt(i);
                    continue;
                }
                
                _activeControllers[i].Tick();
            }
        }
        
        private void OnDestroy()
        {
            StopWaves();
        }
        
        [ContextMenu("Test Spawn First Wave")]
        private void TestSpawnFirstWave()
        {
            if (waves.Count > 0)
            {
                StartWave(0);
            }
        }
    }
    
    [System.Serializable]
    public class EnemyWave
    {
        public string waveName = "Wave";
        public float delayBeforeWave = 0f;
        public List<EnemyWaveEntry> enemies = new List<EnemyWaveEntry>();
    }
    
    [System.Serializable]
    public class EnemyWaveEntry
    {
        public GameObject enemyViewPrefab;
        public EnemyConfigSO config;
        public int count = 5;
        public float spawnInterval = 0.5f;
    }
}
