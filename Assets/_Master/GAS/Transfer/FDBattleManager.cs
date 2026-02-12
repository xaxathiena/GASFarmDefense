using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly IEventBus eventBus;
        private readonly Queue<TowerController> _towersToRemove = new Queue<TowerController>();
        private readonly List<TowerController> _activeTowers = new List<TowerController>();
        public FDBattleManager(
        FDBattleSceneSetting fDBattleSetting,
        IPoolManager poolManager,
        IDebugService debug,
        FDTowerFactory towerFactory,
        IEventBus eventBus)
        {
            // Constructor logic here
            this.fDBattleSetting = fDBattleSetting;
            this.poolManager = poolManager;
            this.debug = debug;
            this.towerFactory = towerFactory;
            this.eventBus = eventBus;
            this.eventBus.Subscribe<EventTowerDestroyed>(OnTowerDestroyed);
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

        public void Start()
        {
            CreateNewTower();
            CreateNewTower();
            debug.Log("FDBattleManager started and tower spawned!", Color.green);
        }

        private void CreateNewTower()
        {
            var tower = poolManager.Spawn<TowerView>(fDBattleSetting.TowerPrefab, fDBattleSetting.TowerSpawnPoint.position, Quaternion.identity);
            var towerController = towerFactory.Create(tower, fDBattleSetting.DefaultTowerData);
            _activeTowers.Add(towerController);
        }

        public void Tick()
        {
            // Tạo bản copy để tránh lỗi "Collection was modified"
            for (int i = 0; i < _activeTowers.Count; i++)
            {
                _activeTowers[i].Tick();
            }
            // 2. Cleanup Logic (Run this AFTER the update loop)
            ProcessPendingRemovals();
        }
        private void ProcessPendingRemovals()
        {
            // If the queue is empty, do nothing (Optimization)
            if (_towersToRemove.Count == 0) return;

            while (_towersToRemove.Count > 0)
            {
                var tower = _towersToRemove.Dequeue();
                
                // Remove from the main logic list
                if (_activeTowers.Remove(tower))
                {
                    // If TowerController implements IDisposable, call it here
                    // tower.Dispose(); 
                }
            }
        }
    }
}