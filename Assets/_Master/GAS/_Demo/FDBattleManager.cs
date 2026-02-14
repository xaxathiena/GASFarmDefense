using System;
using System.Collections.Generic;
using System.Diagnostics;
using FD.Data;
using FD.Views;
using UnityEngine;
using VContainer.Unity;

namespace FD
{
    public class FDBattleManager : IStartable, ITickable
    {
#if UNITY_EDITOR
        // Singleton for Editor/Debug tools ONLY - not available in builds
        public static FDBattleManager Instance { get; private set; }
#endif
        
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
        
        private float _lastEnemySpawnTime = 0f;
        private int _enemiesSpawned = 0;
        
        public FDBattleManager(
        FDBattleSceneSetting fDBattleSetting,
        IPoolManager poolManager,
        IDebugService debug,
        FDTowerFactory towerFactory,
        FDEnemyFactory enemyFactory,
        EnemyManager enemyManager,
        IEventBus eventBus)
        {
#if UNITY_EDITOR
            Instance = this; // Set singleton for Editor tools
#endif
            
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
            // Spawn all towers at once at position (0,0,0)
            for (int i = 0; i < fDBattleSetting.TowerSpawnCount; i++)
            {
                CreateNewTower();
            }
            
            debug.Log($"FDBattleManager started - {fDBattleSetting.TowerSpawnCount} towers spawned!", Color.green);
        }

        private void CreateNewTower()
        {
            var tower = poolManager.Spawn<TowerView>(fDBattleSetting.TowerPrefab, Vector3.zero, Quaternion.identity);
            
            // Random TowerData từ list
            TowerData towerData = null;
            if (fDBattleSetting.TowerDataList != null && fDBattleSetting.TowerDataList.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, fDBattleSetting.TowerDataList.Count);
                towerData = fDBattleSetting.TowerDataList[randomIndex];
            }
            
            var towerController = towerFactory.Create(tower, towerData);
            _activeTowers.Add(towerController);
        }
        
        private void CreateNewEnemy()
        {
            var enemy = poolManager.Spawn<EnemyView>(fDBattleSetting.EnemyPrefab, Vector3.zero, Quaternion.identity);
            
            // Random EnemyData từ list
            EnemyData enemyData = null;
            if (fDBattleSetting.EnemyDataList != null && fDBattleSetting.EnemyDataList.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, fDBattleSetting.EnemyDataList.Count);
                enemyData = fDBattleSetting.EnemyDataList[randomIndex];
            }
            
            var enemyController = enemyFactory.Create(enemy, enemyData);
            _activeEnemies.Add(enemyController);
            enemyManager.RegisterEnemy(enemyController);
            _enemiesSpawned++;
        }

        public void Tick()
        {
            // Spawn enemies với interval
            if (_enemiesSpawned < fDBattleSetting.EnemySpawnCount)
            {
                if (Time.time >= _lastEnemySpawnTime + fDBattleSetting.EnemySpawnInterval)
                {
                    CreateNewEnemy();
                    _lastEnemySpawnTime = Time.time;
                }
            }
            
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
            
            // Update debug stats
            UpdateDebugStats();
        }
        
        private void UpdateDebugStats()
        {
            if (!DebugConfig.EnableDebug) return;
            
            // Aggregate GAS stats
            int totalEffects = 0;
            int totalAbilitiesOnCD = 0;
            int totalGrantedAbilities = 0;
            int effectsApplied = 0;
            int effectsRemoved = 0;
            int abilityActivations = 0;
            int failedActivations = 0;
            int attributeMods = 0;
            int periodicTicks = 0;
            float totalTickTime = 0;
            
            var entityPerformances = new System.Collections.Generic.List<EntityPerformance>();
            
            // Collect from towers
            foreach (var tower in _activeTowers)
            {
                if (tower.AbilitySystemComponent != null)
                {
                    totalEffects += tower.AbilitySystemComponent.GetActiveGameplayEffects().Count;
#if UNITY_EDITOR
                    var cooldowns = tower.AbilitySystemComponent.EditorGetAbilityCooldowns();
                    foreach (var cd in cooldowns)
                    {
                        if (cd.Value > 0) totalAbilitiesOnCD++;
                    }
                    totalGrantedAbilities += tower.AbilitySystemComponent.EditorGetGrantedAbilities().Count;
                    
                    // Get profiler data
                    if (tower.Profiler != null)
                    {
                        effectsApplied += tower.Profiler.EffectsAppliedThisFrame;
                        effectsRemoved += tower.Profiler.EffectsRemovedThisFrame;
                        abilityActivations += tower.Profiler.AbilityActivationsThisFrame;
                        failedActivations += tower.Profiler.FailedActivationsThisFrame;
                        attributeMods += tower.Profiler.AttributeModsThisFrame;
                        periodicTicks += tower.Profiler.PeriodicTicksThisFrame;
                        totalTickTime += tower.Profiler.LastTickTimeMs;
                        
                        var perf = tower.Profiler.GetSnapshot();
                        perf.EntityType = "Tower";
                        entityPerformances.Add(perf);
                    }
#endif
                }
            }
            
            // Collect from enemies
            foreach (var enemy in _activeEnemies)
            {
                if (enemy.AbilitySystemComponent != null)
                {
                    totalEffects += enemy.AbilitySystemComponent.GetActiveGameplayEffects().Count;
#if UNITY_EDITOR
                    var cooldowns = enemy.AbilitySystemComponent.EditorGetAbilityCooldowns();
                    foreach (var cd in cooldowns)
                    {
                        if (cd.Value > 0) totalAbilitiesOnCD++;
                    }
                    totalGrantedAbilities += enemy.AbilitySystemComponent.EditorGetGrantedAbilities().Count;
                    
                    // Get profiler data
                    if (enemy.Profiler != null)
                    {
                        effectsApplied += enemy.Profiler.EffectsAppliedThisFrame;
                        effectsRemoved += enemy.Profiler.EffectsRemovedThisFrame;
                        abilityActivations += enemy.Profiler.AbilityActivationsThisFrame;
                        failedActivations += enemy.Profiler.FailedActivationsThisFrame;
                        attributeMods += enemy.Profiler.AttributeModsThisFrame;
                        periodicTicks += enemy.Profiler.PeriodicTicksThisFrame;
                        totalTickTime += enemy.Profiler.LastTickTimeMs;
                        
                        var perf = enemy.Profiler.GetSnapshot();
                        perf.EntityType = "Enemy";
                        entityPerformances.Add(perf);
                    }
#endif
                }
            }
            
            // Sort by tick time and get top 5 slowest
            entityPerformances.Sort((a, b) => b.TickTimeMs.CompareTo(a.TickTimeMs));
            var topSlow = entityPerformances.Count > 5 
                ? entityPerformances.GetRange(0, 5) 
                : entityPerformances;
            
            // Memory tracking
            long memoryUsage = System.GC.GetTotalMemory(false);
            int gcAlloc = 0; // TODO: Track per-frame GC allocations
            
            // Create GAS stats
            var gasStats = new GASPerformanceStats(
                _activeTowers.Count + _activeEnemies.Count,
                totalEffects,
                effectsApplied,
                effectsRemoved,
                periodicTicks,
                abilityActivations,
                failedActivations,
                totalAbilitiesOnCD,
                totalGrantedAbilities,
                attributeMods,
                0, // attribute callbacks (TODO)
                totalTickTime,
                0, // effect processing time (TODO)
                0, // attribute processing time (TODO)
                memoryUsage,
                gcAlloc,
                topSlow
            );
            
            // Reset frame counters
            foreach (var tower in _activeTowers)
            {
#if UNITY_EDITOR
                tower.Profiler?.ResetFrameCounters();
#endif
            }
            foreach (var enemy in _activeEnemies)
            {
#if UNITY_EDITOR
                enemy.Profiler?.ResetFrameCounters();
#endif
            }
            
            // Send to debug service
            var battleStats = new BattleStats(
                _activeTowers.Count,
                _activeEnemies.Count,
                totalEffects,
                totalAbilitiesOnCD
            );
            debug.UpdateBattleStats(in battleStats);
            debug.UpdateGASStats(in gasStats);
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
        
#if UNITY_EDITOR
        // Public API for Editor/Debug tools
        public List<TowerController> GetAllTowers() => _activeTowers;
        public List<EnemyController> GetAllEnemies() => _activeEnemies;
#endif
    }
}