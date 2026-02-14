using System.Collections.Generic;
using FD.Data;
using FD.Views;
using GAS;
using UnityEngine;
using VContainer;

namespace FD
{
    [System.Serializable]
        public class AbilityInit
        {
            public GameplayAbilityData ability;
            public int level = 1;
            [Tooltip("Passive abilities will be continuously activated (e.g., aura effects)")]
            public bool isPassive = false;
        }
    public class FDBattleSceneSetting : MonoBehaviour
    {
        [Header("Tower Settings")]
        public TowerView TowerPrefab;
        [SerializeField] private List<TowerData> towerDataList = new List<TowerData>();
        [SerializeField] private int towerSpawnCount = 3;
        
        [Header("Enemy Settings")]
        public EnemyView EnemyPrefab;
        [SerializeField] private List<EnemyData> enemyDataList = new List<EnemyData>();
        [SerializeField] private int enemySpawnCount = 5;
        [SerializeField] private float enemySpawnInterval = 0.5f;
        
        [Header("Spawn Points")]
        [SerializeField] private Transform towerSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        
        // Public accessors
        public List<TowerData> TowerDataList => towerDataList;
        public List<EnemyData> EnemyDataList => enemyDataList;
        public int TowerSpawnCount => towerSpawnCount;
        public int EnemySpawnCount => enemySpawnCount;
        public float EnemySpawnInterval => enemySpawnInterval;
        public Transform TowerSpawnPoint => towerSpawnPoint;
        public Transform EnemySpawnPoint => enemySpawnPoint;
        private IDebugService _debug;
        [Inject]
        public void Contruct(IDebugService debug)
        {
            _debug = debug;
        }
        public void Start()
        {
            _debug.AddCommand("Add 1000 Gold", () =>
            {
                _debug.Log($"Gold added! Current: {100}", Color.cyan);
            });
        }
    }
}