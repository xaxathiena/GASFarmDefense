using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Abel.TowerDefense.Render;   // GameRenderManager, RenderGroup
using Abel.TowerDefense.Data;     // UnitRenderData

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

        private readonly ITowerManager       _towerManager;
        private readonly ITowerSpawner        _towerSpawner;
        private readonly IEnemyManager        _enemyManager;
        private readonly IConfigService       _configService;
        private readonly UnitSelectionUIView  _uiView;
        private readonly GameRenderManager    _renderManager;

        // ── Tuning ────────────────────────────────────────────────────────────────

        // Click hit radius in pixels. Larger = easier to click small units.
        private const float ClickRadiusPixels = 50f;

        // World-space Y offset to aim at the unit sprite centre, not its feet.
        // Matches UnitDebugger.unitBodyHeightOffset; adjust to taste.
        private const float BodyHeightOffset = 0.0f;

        // Towers at this tier cannot be merged further (they are already at max evolution).
        private const int MaxMergeTier = 3;

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
        // Live data of the currently selected unit — used for per-frame stat refresh.
        private UnitConfig         _selectedConfig;
        private UnitAttributeSet   _selectedAttributes;
        // ── Constructor ───────────────────────────────────────────────────────────

        public TowerSelectionManager(
            ITowerManager       towerManager,
            ITowerSpawner       towerSpawner,
            IEnemyManager       enemyManager,
            IConfigService      configService,
            UnitSelectionUIView uiView,
            GameRenderManager   renderManager)
        {
            _towerManager  = towerManager;
            _towerSpawner  = towerSpawner;
            _enemyManager  = enemyManager;
            _configService = configService;
            _uiView        = uiView;
            _renderManager = renderManager;
        }

        // ── IInitializable ────────────────────────────────────────────────────────

        public void Initialize()
        {
            // Cache once — Camera.main.FindObjectOfType is expensive per frame.
            _mainCamera = Camera.main;

            if (_mainCamera == null)
                Debug.LogWarning("[TowerSelectionManager] Camera.main not found. Selection will not work.");

            _uiView.Hide();
        }

        // ── ITickable ─────────────────────────────────────────────────────────────

        public void Tick()
        {
            // ── Phase 1: per-frame liveness check + stat refresh ──────────────────
            if (SelectedTower != null || SelectedEnemy != null)
            {
                bool stillAlive;
                if (SelectedTower != null)
                    stillAlive = _towerManager.TryGetTower(SelectedTower.InstanceID, out _);
                else
                    stillAlive = _enemyManager.TryGetEnemy(SelectedEnemy.InstanceID, out _);

                if (!stillAlive)
                {
                    // Unit died or was removed — clear selection immediately.
                    Deselect();
                    return;
                }

                // Unit is still alive — push fresh GAS values to the stat labels.
                if (_selectedAttributes != null)
                    _uiView.RefreshStats(_selectedConfig, _selectedAttributes);
            }

            // ── Phase 2: click detection ──────────────────────────────────────────
            // Only fire on the initial press, not while held.
            if (!Input.GetMouseButtonDown(0))
                return;

            if (_mainCamera == null || _renderManager == null)
                return;

            int    bestInstanceID = -1;
            float  bestDist       = ClickRadiusPixels;
            string bestGroupKey   = "";

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
                        bestDist       = dist;
                        bestInstanceID = dataArray[i].instanceID;
                        bestGroupKey   = kvp.Key;
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
            string                unitTypeID,
            GAS.AbilitySystemComponent asc,
            bool                  isTower,
            Tower                 tower,
            Enemy                 enemy)
        {
            // Update the selected-unit references.
            SelectedTower = tower;
            SelectedEnemy = enemy;

            // Retrieve static balance data.
            var unitsConfig = _configService.GetConfig<UnitsConfig>();
            if (unitsConfig == null || !unitsConfig.TryGetConfig(unitTypeID, out UnitConfig config))
            {
                Debug.LogWarning($"[TowerSelectionManager] No UnitConfig found for '{unitTypeID}'.");
                _uiView.Hide();
                return;
            }

            // Retrieve live GAS attributes — reflects all active buffs/debuffs.
            var attributes = asc?.GetAttributeSet<UnitAttributeSet>();
            if (attributes == null)
            {
                Debug.LogWarning($"[TowerSelectionManager] UnitAttributeSet not found on '{unitTypeID}'.");
                _uiView.Hide();
                return;
            }

            // Cache for per-frame refresh in Tick().
            _selectedConfig     = config;
            _selectedAttributes = attributes;

            _uiView.ShowUnit(config, attributes);
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

            bool sameTier    = configA.Tier == configB.Tier;
            bool belowMaxTier = configA.Tier < MaxMergeTier;

            if (sameTier && belowMaxTier)
            {
                // ── VALID MERGE ──────────────────────────────────────────────────
                ExecuteMerge(towerA, towerB, configA.Tier, unitsConfig);
            }
            else
            {
                // ── INVALID MERGE ────────────────────────────────────────────────
                // Different tiers, or already at the tier cap — cancel merge intent
                // and treat the second click as a plain selection change.
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
            SelectedTower       = null;
            SelectedEnemy       = null;
            _selectedAttributes = null;
            _uiView.Hide();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Merge Execution
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Carries out a confirmed merge:
        ///   1. Finds a random next-tier tower type from UnitsConfig.
        ///   2. Removes both source towers.
        ///   3. Spawns the result at Tower A's position.
        ///   4. Resets selection state.
        /// </summary>
        private void ExecuteMerge(Tower towerA, Tower towerB, int currentTier, UnitsConfig unitsConfig)
        {
            int targetTier = currentTier + 1;

            // Collect all unit IDs whose authored Tier equals targetTier.
            // _mergeCandidates is cleared and reused to avoid allocation.
            string nextTierID = FindNextTierID(unitsConfig, targetTier);

            if (nextTierID == null)
            {
                // No matching next-tier entry in the database — merge is not possible.
                // Fall back to selecting Tower B so the player isn't left in a broken state.
                Debug.LogWarning($"[TowerSelectionManager] Merge aborted: no tower config found " +
                                 $"for Tier {targetTier}. Check UnitsConfig entries.");
                SelectUnit(towerB.TowerID, towerB.ASC, isTower: true, tower: towerB, enemy: null);
                return;
            }

            // Cache Tower A's world position before removal (it will no longer be accessible after).
            Vector3 spawnPosition = towerA.Position;

            Debug.Log($"[TowerSelectionManager] Merging '{towerA.TowerID}' + '{towerB.TowerID}' " +
                      $"→ '{nextTierID}' at {spawnPosition}");

            // Remove both source towers from the field.
            // RemoveTower handles render pipeline cleanup, GAS teardown, and occupied-cell release.
            _towerManager.RemoveTower(towerA.InstanceID);
            _towerManager.RemoveTower(towerB.InstanceID);

            // Spawn the evolved tower at Tower A's original footprint.
            _towerSpawner.SpawnTower(nextTierID, spawnPosition);

            // Clear selection — the new tower is not automatically selected.
            SelectedTower       = null;
            SelectedEnemy       = null;
            _selectedAttributes = null;
            _uiView.Hide();
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
