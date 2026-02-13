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
        [SerializeField] private TowerData defaultTowerData = new TowerData();
        
        [Header("Enemy Settings")]
        public EnemyView EnemyPrefab;
        [SerializeField] private EnemyData defaultEnemyData = new EnemyData();
        
        [Header("Spawn Points")]
        [SerializeField] private Transform towerSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        
        // Public accessors
        public TowerData DefaultTowerData => defaultTowerData;
        public EnemyData DefaultEnemyData => defaultEnemyData;
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