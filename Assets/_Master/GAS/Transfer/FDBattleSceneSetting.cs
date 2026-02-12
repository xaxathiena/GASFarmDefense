using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static FD.Character.TowerBase;

namespace FD
{
    public class FDBattleSceneSetting : MonoBehaviour
    {
        public TowerView TowerPrefab;
        [SerializeField] private Transform towerSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        public Transform TowerSpawnPoint => towerSpawnPoint;
        public Transform EnemySpawnPoint => enemySpawnPoint;
        [Header("Tower Settings")]
        public List<AbilityInit> DefaultTowerAbilities;
        private IDebugService _debug;
        [Inject]
        public void Contruct(IDebugService debug)
        {
            _debug = debug;
            Debug.Log("FDBattleSceneSetting constructed and injected successfully!" + (_debug == null));
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