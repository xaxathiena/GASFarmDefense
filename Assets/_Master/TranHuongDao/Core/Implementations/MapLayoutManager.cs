using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Abel.TowerDefense.Config;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// DOD grid-and-path layout manager for the Tower Defense map.
    /// Uses MapConfigSO to build the grid and multiple enemy paths.
    /// </summary>
    public class MapLayoutManager : MonoBehaviour, IMapLayoutManager
    {
        // ─────────────────────────────────────────────────────────────────────────
        //  Internal DOD data
        // ─────────────────────────────────────────────────────────────────────────

        private GridCellType[] _grid;
        private IReadOnlyList<Vector3>[] _cachedPaths;
        private IConfigService _configService;

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Properties
        // ─────────────────────────────────────────────────────────────────────────

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float CellSize { get; private set; }
        public Vector3 OriginPosition { get; private set; }

        // ─────────────────────────────────────────────────────────────────────────
        //  Lifecycle / VContainer
        // ─────────────────────────────────────────────────────────────────────────

        [Inject]
        public void Construct(IConfigService configService)
        {
            _configService = configService;
        }
        void Start()
        {
            LoadMap("Map_1"); // Temporary default
            
        }
        /// <summary>
        /// Loads map layout configuration from MapConfigSO via ConfigService.
        /// </summary>
        public void LoadMap(string mapID)
        {
            var config = _configService.GetConfig<MapConfigSO>();
            if (config == null)
            {
                Debug.LogError("[MapLayoutManager] Missing MapConfigSO in ConfigService.");
                return;
            }

            // Apply grid dimensions and origin
            GridWidth = config.GridWidth;
            GridHeight = config.GridHeight;
            CellSize = config.CellSize;
            OriginPosition = config.OriginPosition;

            // Initialize every cell as Blocked — only explicitly whitelisted cells are buildable.
            _grid = new GridCellType[GridWidth * GridHeight];
            for (int i = 0; i < _grid.Length; i++)
            {
                _grid[i] = GridCellType.Blocked;
            }

            // Mark designer-whitelisted cells as Buildable.
            if (config.BuildableCells != null)
            {
                foreach (var cellPos in config.BuildableCells)
                {
                    SetCellStateInternal(cellPos, GridCellType.Buildable);
                }
            }

            // Rebuild _cachedPaths and overlay path cells on top (path overrides buildable).
            if (config.EnemyPaths != null)
            {
                _cachedPaths = new IReadOnlyList<Vector3>[config.EnemyPaths.Count];
                for (int i = 0; i < config.EnemyPaths.Count; i++)
                {
                    _cachedPaths[i] = new List<Vector3>(config.EnemyPaths[i].waypoints);
                    MarkPathCells(config.EnemyPaths[i].waypoints);
                }
            }
            else
            {
                _cachedPaths = Array.Empty<IReadOnlyList<Vector3>>();
            }

            Debug.Log($"[MapLayoutManager] Loaded Map {mapID}: {GridWidth}x{GridHeight}, {_cachedPaths.Length} paths.");
        }

        private void MarkPathCells(List<Vector3> waypoints)
        {
            if (waypoints == null || waypoints.Count == 0) return;

            for (int i = 0; i < waypoints.Count; i++)
            {
                SetCellStateInternal(WorldToGridPosition(waypoints[i]), GridCellType.Path);

                if (i >= waypoints.Count - 1) continue;

                Vector3 from = waypoints[i];
                Vector3 to = waypoints[i + 1];
                float length = Vector3.Distance(from, to);
                if (CellSize <= 0f) continue;

                int steps = Mathf.CeilToInt(length / (CellSize * 0.5f));

                for (int s = 1; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    Vector3 sample = Vector3.Lerp(from, to, t);
                    SetCellStateInternal(WorldToGridPosition(sample), GridCellType.Path);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Coordinate conversion
        // ─────────────────────────────────────────────────────────────────────────

        public Vector3 GridToWorldPosition(int x, int y)
        {
            return new Vector3(
                OriginPosition.x + (x + 0.5f) * CellSize,
                OriginPosition.y + (y + 0.5f) * CellSize,
                OriginPosition.z
            );
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            if (CellSize <= 0) return Vector2Int.zero; // Sandbox safety

            int x = Mathf.FloorToInt((worldPosition.x - OriginPosition.x) / CellSize);
            int y = Mathf.FloorToInt((worldPosition.y - OriginPosition.y) / CellSize);
            x = Mathf.Clamp(x, 0, Mathf.Max(0, GridWidth - 1));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, GridHeight - 1));
            return new Vector2Int(x, y);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Cell state
        // ─────────────────────────────────────────────────────────────────────────

        public GridCellType GetCellState(Vector2Int gridPos)
        {
            if (!IsValidCell(gridPos)) return GridCellType.Blocked;
            return _grid[gridPos.y * GridWidth + gridPos.x];
        }

        public void SetCellState(Vector2Int gridPos, GridCellType state)
        {
            SetCellStateInternal(gridPos, state);
        }

        public bool CanBuildAt(Vector3 worldPosition)
        {
            if (_grid == null) return false;
            return GetCellState(WorldToGridPosition(worldPosition)) == GridCellType.Buildable;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Path query
        // ─────────────────────────────────────────────────────────────────────────

        public IReadOnlyList<Vector3>[] GetEnemyPath()
        {
            return _cachedPaths ?? Array.Empty<IReadOnlyList<Vector3>>();
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        private void SetCellStateInternal(Vector2Int gridPos, GridCellType state)
        {
            if (!IsValidCell(gridPos)) return;
            _grid[gridPos.y * GridWidth + gridPos.x] = state;
        }

        private bool IsValidCell(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < GridWidth &&
                   gridPos.y >= 0 && gridPos.y < GridHeight;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Gizmos — Editor-only visualization
        // ─────────────────────────────────────────────────────────────────────────

        private static readonly Color GizmoColorBuildable = new Color(0.20f, 0.80f, 0.20f, 0.35f);
        private static readonly Color GizmoColorPath = new Color(0.90f, 0.30f, 0.30f, 0.50f);
        private static readonly Color GizmoColorBlocked = new Color(0.30f, 0.30f, 0.30f, 0.50f);
        private static readonly Color GizmoColorTowerOccupied = new Color(0.20f, 0.40f, 0.90f, 0.50f);
        private static readonly Color GizmoColorEmpty = new Color(1.00f, 1.00f, 1.00f, 0.10f);
        private static readonly Color GizmoColorGridLine = new Color(1.00f, 1.00f, 1.00f, 0.15f);
        private static readonly Color GizmoColorWaypoint = new Color(1.00f, 0.60f, 0.00f, 1.00f);
        private static readonly Color GizmoColorPathLine = new Color(1.00f, 0.40f, 0.10f, 0.90f);

        private void OnDrawGizmos()
        {
            // Only draw if grid is initialized (to suppress errors outside Play Mode initially)
            if (GridWidth <= 0 || GridHeight <= 0 || CellSize <= 0) return;

            DrawGridGizmos();
            DrawEnemyPathGizmos();
        }

        private void DrawGridGizmos()
        {
            Vector3 cellExtents = new Vector3(CellSize * 0.94f, CellSize * 0.94f, 0.01f);
            Vector3 cellBorder = new Vector3(CellSize, CellSize, 0.01f);

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    Vector3 center = GridToWorldPosition(x, y);

                    GridCellType type = (_grid != null && Application.isPlaying)
                        ? _grid[y * GridWidth + x]
                        : GridCellType.Buildable;

                    Gizmos.color = CellGizmoColor(type);
                    Gizmos.DrawCube(center, cellExtents);

                    Gizmos.color = GizmoColorGridLine;
                    Gizmos.DrawWireCube(center, cellBorder);
                }
            }
        }

        private void DrawEnemyPathGizmos()
        {
            if (_cachedPaths == null || _cachedPaths.Length == 0) return;

            float sphereRadius = CellSize * 0.25f;

            foreach (var path in _cachedPaths)
            {
                if (path == null || path.Count == 0) continue;

                Gizmos.color = GizmoColorPathLine;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Gizmos.DrawLine(path[i], path[i + 1]);
                }

                Gizmos.color = GizmoColorWaypoint;
                foreach (Vector3 wp in path)
                {
                    Gizmos.DrawSphere(wp, sphereRadius);
                }
            }
        }

        private static Color CellGizmoColor(GridCellType type) => type switch
        {
            GridCellType.Buildable => GizmoColorBuildable,
            GridCellType.Path => GizmoColorPath,
            GridCellType.Blocked => GizmoColorBlocked,
            GridCellType.TowerOccupied => GizmoColorTowerOccupied,
            GridCellType.Empty => GizmoColorEmpty,
            _ => GizmoColorEmpty,
        };
    }
}
