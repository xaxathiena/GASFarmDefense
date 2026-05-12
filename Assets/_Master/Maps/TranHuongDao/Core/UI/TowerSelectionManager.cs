using System.Collections.Generic;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Render;   // GameRenderManager, RenderGroup
using FD.Ability;     // UnitRenderData
using UnityEngine;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    // ─────────────────────────────────────────────────────────────────────────────
    // TowerSelectionManager
    //
    // Detects mouse clicks and identifies the closest rendered unit (tower OR
    // enemy) via screen-space proximity — mirroring UnitDebugger's approach.
    //
    // Units are drawn by the GPU-instanced Render2D pipeline and have no
    // GameObjects or Colliders, so Physics.Raycast cannot hit them.
    // Instead, every UnitRenderData in GameRenderManager is projected to screen
    // space and the closest entry within a pixel radius is selected.
    //
    // Registered as IInitializable + ITickable via VContainer RegisterEntryPoint.
    // ─────────────────────────────────────────────────────────────────────────────
    public class TowerSelectionManager : IInitializable, ITickable
    {
        // ── Dependencies ──────────────────────────────────────────────────────────

        private readonly ITowerManager _towerManager;
        private readonly ITowerSpawner _towerSpawner;
        private readonly IEnemyManager _enemyManager;
        private readonly IConfigService _configService;
        private readonly FD.IEventBus _eventBus;
        private readonly GameRenderManager _renderManager;
        private readonly IMapLayoutManager _mapLayoutManager;
        private readonly TowerBuilderConfig _towerBuilderConfig;

        // ── Events ────────────────────────────────────────────────────────────────
        public readonly struct UnitSelectedEvent
        {
            public readonly string UnitID;
            public readonly UnitConfig Config;
            public readonly Abel.GAS.AbilitySystemComponent ASC;
            public readonly int InstanceID;
            public readonly bool IsTower;

            public UnitSelectedEvent(string unitID, UnitConfig config, Abel.GAS.AbilitySystemComponent asc, int instanceID, bool isTower)
            {
                UnitID = unitID;
                Config = config;
                ASC = asc;
                InstanceID = instanceID;
                IsTower = isTower;
            }
        }

        public readonly struct UnitDeselectedEvent { }

        public readonly struct UnitsReadyToMergeEvent
        {
            public readonly Tower TowerA;
            public readonly Tower TowerB;
            public UnitsReadyToMergeEvent(Tower a, Tower b) { TowerA = a; TowerB = b; }
        }

        public readonly struct CancelMergeEvent { }

        // ── Tuning ────────────────────────────────────────────────────────────────

        // Click hit radius in pixels. Larger = easier to click small units.
        private const float ClickRadiusPixels = 50f;

        // World-space Y offset to aim at the unit sprite centre, not its feet.
        // Matches UnitDebugger.unitBodyHeightOffset; adjust to taste.
        private const float BodyHeightOffset = 0.0f;

        // Towers at this tier cannot be merged further (they are already at max evolution).
        // Tier 6 is the current max in UnitsConfig data.
        private const int MaxMergeTier = 6;

        // Reusable list for next-tier candidates — avoids allocation on every merge evaluation.
        private readonly List<string> _mergeCandidates = new List<string>(8);

        // Shared Random instance; seeded once per manager lifetime.
        private readonly System.Random _rng = new System.Random();

        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>The currently selected tower, or null if none is selected.</summary>
        public Tower SelectedTower { get; private set; }

        /// <summary>The currently selected enemy, or null if none is selected.</summary>
        public Enemy SelectedEnemy { get; private set; }

        // Cached Camera.main — resolved once in Initialize() to avoid per-frame lookup.
        private Camera _mainCamera;
        // Cached config of the currently selected unit (static data, safe to cache).
        private UnitConfig _selectedConfig;
        // Cached ASC of the currently selected unit — passed to the UI for the effects panel.
        private Abel.GAS.AbilitySystemComponent _selectedASC;

        // ── Pending Merge State ───────────────────────────────────────────────────
        private Tower _pendingMergeTowerA;
        private Tower _pendingMergeTowerB;
        private int _pendingMergeTier;

        // When a UI button (Merge/Sell) is pressed, set this true so
        // Tick() skips the world raycast on that same frame.
        // Prevents the button click from simultaneously triggering a tower selection change.
        private bool _skipClickThisFrame;

        public TowerSelectionManager(
            ITowerManager towerManager,
            ITowerSpawner towerSpawner,
            IEnemyManager enemyManager,
            IConfigService configService,
            FD.IEventBus eventBus,
            GameRenderManager renderManager,
            IMapLayoutManager mapLayoutManager,
            TowerBuilderConfig towerBuilderConfig)
        {
            _towerManager = towerManager;
            _towerSpawner = towerSpawner;
            _enemyManager = enemyManager;
            _configService = configService;
            _eventBus = eventBus;
            _renderManager = renderManager;
            _mapLayoutManager = mapLayoutManager;
            _towerBuilderConfig = towerBuilderConfig;
        }

        // ── IInitializable ────────────────────────────────────────────────────────

        public void Initialize()
        {
            // Cache once — Camera.main.FindObjectOfType is expensive per frame.
            _mainCamera = Camera.main;

            if (_mainCamera == null)
                Debug.LogWarning("[TowerSelectionManager] Camera.main not found. Selection will not work.");
        }

        // ── ITickable ─────────────────────────────────────────────────────────────

        public void Tick()
        {
            // ── Phase 1: per-frame liveness check + stat refresh ──────────────────
            if (SelectedTower != null)
            {
                if (!_towerManager.TryGetTower(SelectedTower.InstanceID, out Tower liveTower))
                {
                    Deselect();
                    return;
                }
                // Fetch attributes fresh from the live ASC so buffs/debuffs are always current.
                var attrs = liveTower.ASC?.GetAttributeSet<UnitAttributeSet>();
                if (attrs != null)
                {
                    // Update happens in RandomFarmTDView via ticks if needed, or we just rely on event.
                    // If we want to broadcast attribute changes every tick, we could here, but usually UI observes ASC directly.
                }
            }
            else if (SelectedEnemy != null)
            {
                if (!_enemyManager.TryGetEnemy(SelectedEnemy.InstanceID, out Enemy liveEnemy))
                {
                    Deselect();
                    return;
                }
                var attrs = liveEnemy.ASC?.GetAttributeSet<UnitAttributeSet>();
                if (attrs != null)
                {
                    // UI observes ASC directly
                }
            }

            // ── Phase 2: click detection ──────────────────────────────────────────
            // Only fire on the initial press, not while held.
            if (!Input.GetMouseButtonDown(0))
                return;

            Debug.Log("[TowerSelectionManager] Mouse Clicked!");

            // ── Pending merge guard ───────────────────────────────────────────────
            // If there are two towers awaiting merge confirmation, block ALL world
            // raycast processing on this click. Unity's Tick() runs before UI Toolkit
            // dispatches button events. If we allowed the raycast here, it could
            // Deselect() (clearing _pendingMergeTowerA/B) before OnMergeClicked
            // ever fires, causing the merge to silently fail.
            // The player confirms merge via the UI button; we do nothing in Tick()
            // while waiting for that confirmation.
            if (_pendingMergeTowerA != null && _pendingMergeTowerB != null)
            {
                Debug.Log("[TowerSelectionManager] Pending merge active — skipping world raycast this frame.");
                return;
            }

            // Skip this frame's world raycast if a UI button (Merge/Sell) was clicked.
            // UI Toolkit covers the full screen so EventSystem.RaycastAll always returns hits,
            // making it impossible to filter by UI hit count. Instead we use a flag.
            if (_skipClickThisFrame)
            {
                _skipClickThisFrame = false;
                return;
            }
            _skipClickThisFrame = false;

            if (_mainCamera == null || _renderManager == null)
            {
                Debug.LogWarning("[TowerSelectionManager] Camera or RenderManager is null!");
                return;
            }

            int bestInstanceID = -1;
            float bestDist = ClickRadiusPixels;
            string bestGroupKey = "";

            Vector2 mousePos = Input.mousePosition;

            // Iterate every render group (one per unit type) and every live instance.
            // This mirrors exactly what UnitDebugger.SelectUnitUnderMouse does.
            foreach (var kvp in _renderManager.LoadedRenderGroups)
            {
                var dataArray = kvp.Value.GetRenderData();

                for (int i = 0; i < dataArray.Length; i++)
                {
                    // instanceID == 0 means the slot is unused.
                    if (dataArray[i].instanceID == 0) continue;

                    // Reconstruct world position: the render system stores XZ as float2.
                    // HeightOffset centres the click target on the sprite body.
                    Vector3 worldPos = new Vector3(
                        dataArray[i].position.x,
                        BodyHeightOffset * dataArray[i].scale,
                        dataArray[i].position.y);

                    // Skip units that are behind the camera.
                    Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
                    if (screenPos.z < 0f) continue;

                    float dist = Vector2.Distance(mousePos, new Vector2(screenPos.x, screenPos.y));

                    if (dist < bestDist)
                    {
                        // Verify this instance actually exists in logic managers
                        // to prevent selecting "ghost" frames from recently destroyed units
                        // which would block selecting the new merged tower underneath.
                        if (_towerManager.TryGetTower(dataArray[i].instanceID, out _) ||
                            _enemyManager.TryGetEnemy(dataArray[i].instanceID, out _))
                        {
                            bestDist = dist;
                            bestInstanceID = dataArray[i].instanceID;
                            bestGroupKey = kvp.Key;
                        }
                    }
                }
            }

            if (bestInstanceID == -1)
            {
                Deselect();
                return;
            }

            // ── Identify whether the hit instance is a Tower or an Enemy ──────────
            // Tower IDs and Enemy IDs live in separate managers; try each in order.

            if (_towerManager.TryGetTower(bestInstanceID, out Tower tower))
            {
                // ── Merge check: only applicable when a tower is already selected ──────
                if (SelectedTower != null)
                {
                    if (SelectedTower.InstanceID == tower.InstanceID)
                    {
                        // Player clicked the same tower again — keep it selected, do nothing.
                        return;
                    }

                    // Player clicked a different tower while one is already selected.
                    // Evaluate merge eligibility before switching selection.
                    CheckMerge(SelectedTower, tower);
                    return;
                }

                // No previous tower selected — straight selection.
                SelectUnit(tower.TowerID, tower.ASC, isTower: true, tower: tower, enemy: null);
                return;
            }

            if (_enemyManager.TryGetEnemy(bestInstanceID, out Enemy enemy))
            {
                SelectUnit(enemy.EnemyID, enemy.ASC, isTower: false, tower: null, enemy: enemy);
                return;
            }

            // The instance exists in the renderer but is unknown to logic managers
            // (e.g. a bullet or decorative sprite). Treat as empty space.
            Deselect();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Shared selection path for towers and enemies.
        /// Looks up the UnitConfig by unit type ID and the live UnitAttributeSet
        /// from the ASC, then pushes both to the UI view.
        /// </summary>
        private void SelectUnit(
            string unitTypeID,
            Abel.GAS.AbilitySystemComponent asc,
            bool isTower,
            Tower tower,
            Enemy enemy)
        {
            // Update the selected-unit references.
            SelectedTower = tower;
            SelectedEnemy = enemy;

            // Retrieve static balance data.
            var unitsConfig = _configService.GetConfig<UnitsConfig>();
            if (unitsConfig == null || !unitsConfig.TryGetConfig(unitTypeID, out UnitConfig config))
            {
                Debug.LogWarning($"[TowerSelectionManager] No UnitConfig found for '{unitTypeID}'.");
                Deselect();
                return;
            }

            // Retrieve live GAS attributes — reflects all active buffs/debuffs.
            var attributes = asc?.GetAttributeSet<UnitAttributeSet>();
            if (attributes == null)
            {
                Debug.LogWarning($"[TowerSelectionManager] UnitAttributeSet not found on '{unitTypeID}'.");
                Deselect();
                return;
            }

            // Cache static config and ASC for per-frame refresh in Tick().
            _selectedConfig = config;
            _selectedASC = asc;

            int instanceID = isTower ? tower.InstanceID : enemy.InstanceID;
            _eventBus.Publish(new UnitSelectedEvent(unitTypeID, config, asc, instanceID, isTower));
        }

        /// <summary>
        /// Evaluates whether Tower A (currently selected) and Tower B (just clicked)
        /// satisfy the merge conditions. Branches:
        ///   VALID   → logs the merge opportunity (actual merge logic TBD).
        ///   INVALID → cancels merge intent and switches selection to Tower B.
        /// </summary>
        private void CheckMerge(Tower towerA, Tower towerB)
        {
            var unitsConfig = _configService.GetConfig<UnitsConfig>();

            // Declare upfront: out-params from separate if-branches are not
            // considered definitely assigned by the C# flow analyser.
            UnitConfig configA = default;
            UnitConfig configB = default;

            // Both towers must have authored config data to compare tiers.
            bool hasConfigA = unitsConfig != null && unitsConfig.TryGetConfig(towerA.TowerID, out configA);
            bool hasConfigB = unitsConfig != null && unitsConfig.TryGetConfig(towerB.TowerID, out configB);

            if (!hasConfigA || !hasConfigB)
            {
                // Missing config — cannot evaluate merge; fall back to selecting Tower B.
                Debug.LogWarning($"[TowerSelectionManager] Merge check failed: missing config for " +
                                 $"'{towerA.TowerID}' or '{towerB.TowerID}'.");
                SelectUnit(towerB.TowerID, towerB.ASC, isTower: true, tower: towerB, enemy: null);
                return;
            }

            bool sameType = towerA.TowerID == towerB.TowerID;
            bool sameTier = configA.Tier == configB.Tier;
            bool belowMaxTier = configA.Tier < MaxMergeTier;

            if (sameType && sameTier && belowMaxTier)
            {
                // ── VALID MERGE ──────────────────────────────────────────────────
                // Prepare manual merge state and notify the UI to show the Merge button.
                _pendingMergeTowerA = towerA;
                _pendingMergeTowerB = towerB;
                _pendingMergeTier = configA.Tier;

                // Select Tower B so its stats show up on the UI while waiting for the merge decision
                SelectUnit(towerB.TowerID, towerB.ASC, isTower: true, tower: towerB, enemy: null);

                _eventBus.Publish(new UnitsReadyToMergeEvent(towerA, towerB));
            }
            else
            {
                // ── INVALID MERGE ────────────────────────────────────────────────
                // Different tiers, or already at the tier cap — cancel merge intent
                // and treat the second click as a plain selection change.
                _pendingMergeTowerA = null;
                _pendingMergeTowerB = null;
                _eventBus.Publish(new CancelMergeEvent());
                if (!sameTier)
                    Debug.Log($"[TowerSelectionManager] Merge cancelled: tier mismatch " +
                              $"(A={configA.Tier}, B={configB.Tier}).");
                else
                    Debug.Log($"[TowerSelectionManager] Merge cancelled: Tower B is already " +
                              $"at max tier ({MaxMergeTier}).");

                SelectUnit(towerB.TowerID, towerB.ASC, isTower: true, tower: towerB, enemy: null);
            }
        }

        /// <summary>Clears selection state and hides the info panel.</summary>
        private void Deselect()
        {
            SelectedTower = null;
            SelectedEnemy = null;
            _selectedASC = null;

            _pendingMergeTowerA = null;
            _pendingMergeTowerB = null;
            _eventBus.Publish(new CancelMergeEvent());

            _eventBus.Publish(new UnitDeselectedEvent());
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Merge Execution
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Call this from a UI button handler to prevent Tick() from processing
        /// the same mouse click as a world-space tower selection event.
        /// </summary>
        public void BlockClickThisFrame() => _skipClickThisFrame = true;

        /// <summary>
        /// Carries out a confirmed pending merge initiated by the UI.
        /// Uses IDs to survive UI click-through which might clear the immediate selection state.
        /// </summary>
        public void ExecuteMergeByID(int instanceIdA, int instanceIdB)
        {
            // Block world raycast this frame — the Merge button click must not
            // simultaneously trigger a CheckMerge or Deselect in Tick().
            _skipClickThisFrame = true;

            if (!_towerManager.TryGetTower(instanceIdA, out Tower towerA) ||
                !_towerManager.TryGetTower(instanceIdB, out Tower towerB))
            {
                Debug.LogWarning("[TowerSelectionManager] Merge failed: One or both towers no longer exist.");
                Deselect();
                return;
            }

            var unitsConfig = _configService.GetConfig<UnitsConfig>();
            if (unitsConfig == null) return;

            unitsConfig.TryGetConfig(towerA.TowerID, out UnitConfig configA);
            int targetTier = configA.Tier + 1;

            string nextTierID = FindNextTierID(unitsConfig, targetTier);

            if (string.IsNullOrEmpty(nextTierID))
            {
                Debug.LogWarning($"[TowerSelectionManager] Merge aborted: no tower found " +
                                 $"for Tier {targetTier} in UnitsConfig. Add Tier {targetTier} entries to fix this.");
                Deselect();
                return;
            }

            Vector3 spawnPosition = towerB.Position;

            Debug.Log($"[TowerSelectionManager] Merging '{towerA.TowerID}' + '{towerB.TowerID}' " +
                      $"→ '{nextTierID}' at {spawnPosition}");

            // Remove both source towers first (this marks their cells as Buildable via DestroyTower).
            _towerManager.RemoveTower(towerA.InstanceID);
            _towerManager.RemoveTower(towerB.InstanceID);

            // Re-mark towerB's cell as TowerOccupied for the new merged tower.
            // SpawnTower (via SpawnTowerInternal) updates occupiedCells but doesn't call SetCellState,
            // so we do it here to keep mapLayoutManager consistent.
            if (_mapLayoutManager != null)
            {
                var gridPos = _mapLayoutManager.WorldToGridPosition(spawnPosition);
                _mapLayoutManager.SetCellState(gridPos, GridCellType.TowerOccupied);
            }

            _towerSpawner.SpawnTower(nextTierID, spawnPosition);

            Deselect();
        }

        /// <summary>
        /// Scans <paramref name="unitsConfig"/> for entries whose Tier equals
        /// <paramref name="targetTier"/> and returns one at random.
        /// Returns null when no matching entry is found.
        /// </summary>
        private string FindNextTierID(UnitsConfig unitsConfig, int targetTier)
        {
            _mergeCandidates.Clear();

            foreach (var entry in unitsConfig.unitEntries)
            {
                if (entry.Tier == targetTier && !string.IsNullOrEmpty(entry.UnitID))
                    _mergeCandidates.Add(entry.UnitID);
            }

            if (_mergeCandidates.Count == 0)
                return null;

            // Pick uniformly at random so every same-tier result has equal probability.
            return _mergeCandidates[_rng.Next(_mergeCandidates.Count)];
        }
    }
}

