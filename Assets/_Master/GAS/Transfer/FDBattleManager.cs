using System;
using System.Collections.Generic;
using System.Diagnostics;
using FD.Views;
using UnityEngine;
using VContainer.Unity;

namespace FD
{
    public class FDBattleManager : IStartable, ITickable
    {
        private readonly FDBattleSceneSetting fDBattleSetting;
        private readonly IPoolManager poolManager;
        private readonly IDebugService debug;
        private readonly FDTowerFactory towerFactory;
        private readonly FDEnemyFactory enemyFactory;
        private readonly EnemyManager enemyManager;
        private readonly IEventBus eventBus;
        private readonly Queue<TowerController> _towersToRemove = new Queue<TowerController>();
        private readonly List<TowerController> _activeTowers = new List<TowerController>();
        private readonly Queue<EnemyController> _enemiesToRemove = new Queue<EnemyController>();
        private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();
        
        public FDBattleManager(
        FDBattleSceneSetting fDBattleSetting,
        IPoolManager poolManager,
        IDebugService debug,
        FDTowerFactory towerFactory,
        FDEnemyFactory enemyFactory,
        EnemyManager enemyManager,
        IEventBus eventBus)
        {
            // Constructor logic here
            this.fDBattleSetting = fDBattleSetting;
            this.poolManager = poolManager;
            this.debug = debug;
            this.towerFactory = towerFactory;
            this.enemyFactory = enemyFactory;
            this.enemyManager = enemyManager;
            this.eventBus = eventBus;
            this.eventBus.Subscribe<EventTowerDestroyed>(OnTowerDestroyed);
            this.eventBus.Subscribe<EventEnemyDestroyed>(OnEnemyDestroyed);
        }
        private void OnTowerDestroyed(EventTowerDestroyed destroyed)
        {
            debug.Log($"Tower {destroyed.TowerId} destroyed at position {destroyed.Position}", Color.yellow);
            var tower = _activeTowers.Find(t => t.Id == destroyed.TowerId);
            if (tower != null)
            {
                // FIX: Do NOT remove from _activeTowers immediately.
                // Add to a "pending removal" queue instead.
                _towersToRemove.Enqueue(tower);
            }
        }
        
        private void OnEnemyDestroyed(EventEnemyDestroyed destroyed)
        {
            debug.Log($"Enemy {destroyed.EnemyId} destroyed at position {destroyed.Position}", Color.yellow);
            var enemy = _activeEnemies.Find(e => e.Id == destroyed.EnemyId);
            if (enemy != null)
            {
                _enemiesToRemove.Enqueue(enemy);
                enemyManager.UnregisterEnemy(enemy);
            }
        }

        public void Start()
        {
            CreateNewTower();
            CreateNewEnemy();
            CreateNewEnemy();
            debug.Log("FDBattleManager started - towers and enemies spawned!", Color.green);
        }

        private void CreateNewTower()
        {
            var tower = poolManager.Spawn<TowerView>(fDBattleSetting.TowerPrefab, fDBattleSetting.TowerSpawnPoint.position, Quaternion.identity);
            var towerController = towerFactory.Create(tower, fDBattleSetting.DefaultTowerData);
            _activeTowers.Add(towerController);
        }
        
        private void CreateNewEnemy()
        {
            var enemy = poolManager.Spawn<EnemyView>(fDBattleSetting.EnemyPrefab, fDBattleSetting.EnemySpawnPoint.position, Quaternion.identity);
            var enemyController = enemyFactory.Create(enemy, fDBattleSetting.DefaultEnemyData);
            _activeEnemies.Add(enemyController);
            enemyManager.RegisterEnemy(enemyController);
        }

        public void Tick()
        {
            // Tick towers
            for (int i = 0; i < _activeTowers.Count; i++)
            {
                _activeTowers[i].Tick();
            }
            
            // Tick enemies
            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                _activeEnemies[i].Tick();
            }
            
            // Cleanup
            ProcessPendingRemovals();
        }
        private void ProcessPendingRemovals()
        {
            // Remove towers
            if (_towersToRemove.Count > 0)
            {
                while (_towersToRemove.Count > 0)
                {
                    var tower = _towersToRemove.Dequeue();
                    _activeTowers.Remove(tower);
                }
            }
            
            // Remove enemies
            if (_enemiesToRemove.Count > 0)
            {
                while (_enemiesToRemove.Count > 0)
                {
                    var enemy = _enemiesToRemove.Dequeue();
                    _activeEnemies.Remove(enemy);
                }
            }
        }
    }
}