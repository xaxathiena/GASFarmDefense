using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FD.Character
{
    public class FDEnemyWaveController : MonoBehaviour
    {
        [Header("Spawn & Path")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform[] pathPoints;

        [Header("Wave Settings")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private float timeBetweenWaves = 2f;
        [SerializeField] private List<FDEnemyWave> waves = new List<FDEnemyWave>();

        private Coroutine waveRoutine;

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
            waveRoutine = StartCoroutine(RunAllWaves());
        }

        public void StartWave(int waveIndex)
        {
            StopWaves();
            waveRoutine = StartCoroutine(RunSingleWave(waveIndex));
        }

        public void StopWaves()
        {
            if (waveRoutine != null)
            {
                StopCoroutine(waveRoutine);
                waveRoutine = null;
            }
        }

        private IEnumerator RunAllWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                yield return RunWave(waves[i]);
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        private IEnumerator RunSingleWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waves.Count)
            {
                yield break;
            }

            yield return RunWave(waves[waveIndex]);
        }

        private IEnumerator RunWave(FDEnemyWave wave)
        {
            if (wave == null)
            {
                yield break;
            }

            if (wave.delayBeforeWave > 0f)
            {
                yield return new WaitForSeconds(wave.delayBeforeWave);
            }

            foreach (var entry in wave.enemies)
            {
                if (entry == null || entry.enemyPrefab == null)
                {
                    continue;
                }

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

        private void SpawnEnemy(FDEnemyWaveEntry entry)
        {
            Vector3 spawnPosition = spawnPoint != null
                ? spawnPoint.position
                : (pathPoints != null && pathPoints.Length > 0 ? pathPoints[0].position : Vector3.zero);

            var enemy = Instantiate(entry.enemyPrefab, spawnPosition, Quaternion.identity);
            enemy.InitializePath(pathPoints);
        }
    }

    [System.Serializable]
    public class FDEnemyWave
    {
        public string waveName = "Wave";
        public float delayBeforeWave = 0f;
        public List<FDEnemyWaveEntry> enemies = new List<FDEnemyWaveEntry>();
    }

    [System.Serializable]
    public class FDEnemyWaveEntry
    {
        public FDEnemyBase enemyPrefab;
        public int count = 5;
        public float spawnInterval = 0.5f;
    }
}
