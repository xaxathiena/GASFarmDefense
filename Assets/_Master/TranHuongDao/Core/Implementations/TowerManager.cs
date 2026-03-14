using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using GAS;
using System.Linq;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Manages tower placement, ticking, and removal.
    ///
    /// On <see cref="Start"/> it spawns three towers at hardcoded positions to
    /// bootstrap the game.  Runtime placement goes through <see cref="PlaceTower"/>.
    ///
    /// Registered as ITickable + IStartable + IDisposable via RegisterEntryPoint.
    /// </summary>
    public class TowerManager : ITowerManager, ITowerSpawner, ITickable, IDisposable
    {
        // ── Dependencies ─────────────────────────────────────────────────────────
        private readonly IObjectResolver container;
        private readonly IRender2DService renderService;
        private readonly IConfigService configService;
        /// <summary>
        /// Service for generating unique instance IDs for towers.  Used for render mapping and GAS targeting.
        /// </summary>
        private readonly IInstanceIDService instanceIDService;

        private readonly FD.IEventBus eventBus;
        private readonly FD.Modules.VFX.IVFXManager vfxManager;

        // ── Tower registry ────────────────────────────────────────────────────────
        private readonly Dictionary<int, Tower> activeTowers = new Dictionary<int, Tower>(32);
        private readonly List<int> activeIDBuffer = new List<int>(32);
        private readonly HashSet<Vector3> occupiedCells = new HashSet<Vector3>();

        // ── IEnemyManager events ─────────────────────────────────────────────────
        public event Action<int> OnTowerPlaced;
        public event Action<int> OnTowerRemoved;

        // ── Counters ─────────────────────────────────────────────────────────────
        public int ActiveTowerCount => activeTowers.Count;

        // ─────────────────────────────────────────────────────────────────────────
        public TowerManager(
            IObjectResolver container,
            IRender2DService renderService,
            IConfigService configService,
            IInstanceIDService instanceIDService,
            FD.IEventBus eventBus,
            FD.Modules.VFX.IVFXManager vfxManager)
        {
            this.container = container;
            this.renderService = renderService;
            this.configService = configService;
            this.instanceIDService = instanceIDService;
            this.eventBus = eventBus;
            this.vfxManager = vfxManager;
        }

        public void Tick()
        {
            float dt = Time.deltaTime;
            foreach (var tower in activeTowers.Values)
                tower.Tick(dt);
        }

        public void Dispose()
        {
            var towers = new List<Tower>(activeTowers.Values);
            foreach (var t in towers)
                DestroyTower(t, notify: false);
            activeTowers.Clear();
        }

        // ── ITowerManager ─────────────────────────────────────────────────────────

        public int PlaceTower(string towerID, Vector3 gridPosition)
        {
            if (!IsCellAvailable(gridPosition))
            {
                Debug.LogWarning($"[TowerManager] Cell {gridPosition} is already occupied.");
                return -1;
            }
            return SpawnTowerInternal(towerID, gridPosition);
        }

        public void RemoveTower(int instanceID)
        {
            if (activeTowers.TryGetValue(instanceID, out var tower))
                DestroyTower(tower, notify: true);
        }

        public IReadOnlyList<int> GetActiveTowerIDs()
        {
            activeIDBuffer.Clear();
            activeIDBuffer.AddRange(activeTowers.Keys);
            return activeIDBuffer;
        }

        public bool TryGetTowerPosition(int instanceID, out Vector3 position)
        {
            if (activeTowers.TryGetValue(instanceID, out var tower))
            {
                position = tower.Position;
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        public bool IsCellAvailable(Vector3 gridPosition) => !occupiedCells.Contains(gridPosition);

        public bool TryGetTower(int instanceID, out Tower tower)
            => activeTowers.TryGetValue(instanceID, out tower);

        public void GetTowersInRange(Vector3 center, float radius, List<GAS.AbilitySystemComponent> ignoreList, List<GAS.AbilitySystemComponent> results, int maxCount = int.MaxValue)
        {
            float sqrRadius = radius * radius;

            // 1. Collect candidates that are actually in range first.
            var candidates = new List<Tower>();
            foreach (var tower in activeTowers.Values)
            {
                if ((tower.Position - center).sqrMagnitude <= sqrRadius && !ignoreList.Contains(tower.ASC))
                {
                    candidates.Add(tower);
                }
            }

            if (candidates.Count == 0) return;

            // 2. If we have fewer candidates than requested, just return all of them.
            if (candidates.Count <= maxCount)
            {
                foreach (var c in candidates) results.Add(c.ASC);
                return;
            }

            // 3. Otherwise, pick 'maxCount' random candidates using a partial Fisher-Yates shuffle.
            for (int i = 0; i < maxCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, candidates.Count);
                (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);

                results.Add(candidates[i].ASC);
            }
        }



        /// <summary>
        /// Spawns a tower without re-checking cell availability.
        /// The caller (e.g. TowerDragDropManager) is responsible for validating
        /// via CanBuildAt and marking the cell with SetCellState before calling this.
        /// </summary>
        public void SpawnTower(string unitID, Vector3 position)
            => SpawnTowerInternal(unitID, position);

        // ── Private helpers ──────────────────────────────────────────────────────

        private int SpawnTowerInternal(string towerID, Vector3 position)
        {
            // Look up authored stats from the shared config database.
            var unitsConfig = configService.GetConfig<UnitsConfig>();
            if (unitsConfig == null || !unitsConfig.TryGetConfig(towerID, out UnitConfig config))
            {
                Debug.LogWarning($"[TowerManager] No UnitConfigData found for '{towerID}'. Tower not spawned.");
                return -1;
            }

            // Resolve a fresh Transient AbilitySystemComponent for this tower.
            var asc = container.Resolve<AbilitySystemComponent>();
            var behaviourRegistry = container.Resolve<AbilityBehaviourRegistry>();
            var tower = new Tower(asc, behaviourRegistry);
            int id = instanceIDService.GetNextID();

            // Look up the attack ability asset from the central AbilitiesConfig registry.
            // The ID stored in the unit config must match the SO asset name exactly.
            GameplayAbilityData attackAbility = null;
            GameplayAbilityData skillAbility = null;

            if (!string.IsNullOrEmpty(config.AttackAbilityID) || !string.IsNullOrEmpty(config.SkillAbilityID))
            {
                var abilitiesConfig = configService.GetConfig<AbilitiesConfig>();

                if (!string.IsNullOrEmpty(config.AttackAbilityID))
                    if (abilitiesConfig == null || !abilitiesConfig.TryGetAbility(config.AttackAbilityID, out attackAbility))
                        Debug.LogWarning($"[TowerManager] Attack ability '{config.AttackAbilityID}' not found in AbilitiesConfig for unit '{towerID}'.");

                if (!string.IsNullOrEmpty(config.SkillAbilityID))
                    if (abilitiesConfig == null || !abilitiesConfig.TryGetAbility(config.SkillAbilityID, out skillAbility))
                        Debug.LogWarning($"[TowerManager] Skill ability '{config.SkillAbilityID}' not found in AbilitiesConfig for unit '{towerID}'.");
            }

            var tagVFXConfig = configService.GetConfig<TagVFXConfig>();

            tower.Initialize(id, towerID, position, config, attackAbility, skillAbility, renderService, eventBus, vfxManager, tagVFXConfig);
            tower.OnDestroyed += HandleTowerDestroyed;

            // Attach a physics collider and TowerClickProxy to the proxy GameObject so that
            // 3D raycasts in TowerSelectionManager can identify which Tower was clicked.
            if (tower.ProxyTransform != null)
            {
                tower.ProxyTransform.gameObject.AddComponent<BoxCollider>();
                var clickProxy = tower.ProxyTransform.gameObject.AddComponent<TowerClickProxy>();
                clickProxy.InstanceID = id;
            }

            activeTowers.Add(id, tower);
            occupiedCells.Add(position);
            OnTowerPlaced?.Invoke(id);

            Debug.Log($"[TowerManager] Placed tower '{towerID}' at {position} [ID:{id}]");
            return id;
        }

        private void DestroyTower(Tower tower, bool notify)
        {
            tower.OnDestroyed -= HandleTowerDestroyed;
            tower.Cleanup();
            activeTowers.Remove(tower.InstanceID);
            occupiedCells.Remove(tower.Position);

            if (notify) OnTowerRemoved?.Invoke(tower.InstanceID);
        }

        private void HandleTowerDestroyed(Tower tower)
        {
            DestroyTower(tower, notify: true);
        }
    }
}
