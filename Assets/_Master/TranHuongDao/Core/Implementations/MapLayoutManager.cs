using System;
using System.Collections.Generic;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// DOD grid-and-path layout manager for the Tower Defense map.
    ///
    /// Design principles:
    ///   • Pure Data — the grid is a flat <see cref="GridCellType"/> array (no GameObjects per cell).
    ///   • MonoBehaviour — Inspector configuration + OnDrawGizmos support.
    ///   • VContainer — registered via RegisterComponent; all systems depend on <see cref="IMapLayoutManager"/>.
    ///   • UnitRender-ready — exposes only mathematical Vector3/Vector2Int data;
    ///     visual rendering is delegated to the UnitRender system.
    ///   • GAS-ready — <see cref="CanBuildAt"/> and <see cref="SetCellState"/> are the
    ///     entry points for "Build Tower" abilities.
    /// </summary>
    public class MapLayoutManager : MonoBehaviour, IMapLayoutManager
    {
        // ─────────────────────────────────────────────────────────────────────────
        //  Inspector settings
        // ─────────────────────────────────────────────────────────────────────────

        [Header("Grid Settings")]
        [Tooltip("Number of columns in the grid.")]
        [SerializeField] private int gridWidth = 20;

        [Tooltip("Number of rows in the grid.")]
        [SerializeField] private int gridHeight = 10;

        [Tooltip("World-space size of one cell (square).")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("World-space position of the bottom-left corner of the grid.")]
        [SerializeField] private Vector3 originPosition = Vector3.zero;

        [Header("Enemy Path Waypoints")]
        [Tooltip("Ordered world-space positions the enemy follows. " +
                 "Every grid cell intersected by segments is automatically marked as Path.")]
        [SerializeField] private Vector3[] enemyPathWaypoints = Array.Empty<Vector3>();

        // ─────────────────────────────────────────────────────────────────────────
        //  Internal DOD data
        // ─────────────────────────────────────────────────────────────────────────

        // Row-major flat array: index = y * gridWidth + x.
        // Avoids 2D-array allocation overhead and keeps data cache-friendly.
        private GridCellType[] _grid;

        // Cached multi-lane path view for GetPaths() — lane 0 wraps enemyPathWaypoints.
        private IReadOnlyList<Vector3>[] _cachedPaths;

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Properties
        // ─────────────────────────────────────────────────────────────────────────

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;

        // ─────────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            InitializeGrid();
        }

        /// <summary>
        /// Allocates the flat grid array, sets every cell to Buildable,
        /// then marks cells that overlap the enemy path as Path.
        /// </summary>
        private void InitializeGrid()
        {
            _grid = new GridCellType[gridWidth * gridHeight];

            // Default: every cell is available for building.
            for (int i = 0; i < _grid.Length; i++)
                _grid[i] = GridCellType.Buildable;

            // Overlay path cells.
            MarkPathCells();

            // Build the GetPaths() cache (lane 0 = enemyPathWaypoints).
            RebuildPathCache();

            Debug.Log($"[MapLayoutManager] Grid initialized: {gridWidth}x{gridHeight} " +
                      $"cells, origin={originPosition}, cellSize={cellSize}. " +
                      $"Waypoints: {enemyPathWaypoints.Length}.");
        }

        /// <summary>
        /// Samples each waypoint segment at half-cell granularity and marks
        /// every touched cell as <see cref="GridCellType.Path"/>.
        /// This is a pure data pass — no GameObjects are created.
        /// </summary>
        private void MarkPathCells()
        {
            for (int i = 0; i < enemyPathWaypoints.Length; i++)
            {
                // Mark the waypoint's own cell.
                SetCellStateInternal(WorldToGridPosition(enemyPathWaypoints[i]), GridCellType.Path);

                if (i >= enemyPathWaypoints.Length - 1) continue;

                // Sample the segment at sub-cell intervals to avoid missing cells.
                Vector3 from = enemyPathWaypoints[i];
                Vector3 to = enemyPathWaypoints[i + 1];
                float length = Vector3.Distance(from, to);
                int steps = Mathf.CeilToInt(length / (cellSize * 0.5f));

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

        /// <summary>
        /// Returns the world-space center of the cell at column <paramref name="x"/>,
        /// row <paramref name="y"/>.
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            return new Vector3(
                originPosition.x + (x + 0.5f) * cellSize,
                originPosition.y + (y + 0.5f) * cellSize,
                originPosition.z
            );
        }

        /// <summary>
        /// Maps a world-space position to its enclosing grid cell.
        /// The result is always clamped to [0, width-1] x [0, height-1].
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
            int y = Mathf.FloorToInt((worldPosition.y - originPosition.y) / cellSize);
            x = Mathf.Clamp(x, 0, gridWidth - 1);
            y = Mathf.Clamp(y, 0, gridHeight - 1);
            return new Vector2Int(x, y);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Cell state
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public GridCellType GetCellState(Vector2Int gridPos)
        {
            if (!IsValidCell(gridPos)) return GridCellType.Blocked;
            return _grid[gridPos.y * gridWidth + gridPos.x];
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Called by GAS abilities, e.g.:
        /// <code>mapLayout.SetCellState(cell, GridCellType.TowerOccupied);</code>
        /// Out-of-bounds writes are silently discarded.
        /// </remarks>
        public void SetCellState(Vector2Int gridPos, GridCellType state)
        {
            SetCellStateInternal(gridPos, state);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – GAS / Build query
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// GAS "Build Tower" ability usage pattern:
        /// <code>
        /// if (!mapLayout.CanBuildAt(hitWorldPos)) { CancelAbility(); return; }
        /// mapLayout.SetCellState(mapLayout.WorldToGridPosition(hitWorldPos), GridCellType.TowerOccupied);
        /// </code>
        /// </remarks>
        public bool CanBuildAt(Vector3 worldPosition)
        {
            if (_grid == null) return false;
            return GetCellState(WorldToGridPosition(worldPosition)) == GridCellType.Buildable;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Path query
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public IReadOnlyList<Vector3>[] GetEnemyPath()
        {
            if (_cachedPaths == null) RebuildPathCache();
            return _cachedPaths;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        private void SetCellStateInternal(Vector2Int gridPos, GridCellType state)
        {
            if (!IsValidCell(gridPos)) return;
            _grid[gridPos.y * gridWidth + gridPos.x] = state;
        }

        private bool IsValidCell(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridWidth &&
                   gridPos.y >= 0 && gridPos.y < gridHeight;
        }

        private void RebuildPathCache()
        {
            _cachedPaths = new IReadOnlyList<Vector3>[]
            {
                new List<Vector3>(enemyPathWaypoints),
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Gizmos — Editor-only visualization
        // ─────────────────────────────────────────────────────────────────────────

        // Cell fill colours
        private static readonly Color GizmoColorBuildable = new Color(0.20f, 0.80f, 0.20f, 0.35f);
        private static readonly Color GizmoColorPath = new Color(0.90f, 0.30f, 0.30f, 0.50f);
        private static readonly Color GizmoColorBlocked = new Color(0.30f, 0.30f, 0.30f, 0.50f);
        private static readonly Color GizmoColorTowerOccupied = new Color(0.20f, 0.40f, 0.90f, 0.50f);
        private static readonly Color GizmoColorEmpty = new Color(1.00f, 1.00f, 1.00f, 0.10f);

        // Grid and path overlay colours
        private static readonly Color GizmoColorGridLine = new Color(1.00f, 1.00f, 1.00f, 0.15f);
        private static readonly Color GizmoColorWaypoint = new Color(1.00f, 0.60f, 0.00f, 1.00f);
        private static readonly Color GizmoColorPathLine = new Color(1.00f, 0.40f, 0.10f, 0.90f);

        private void OnDrawGizmos()
        {
            DrawGridGizmos();
            DrawEnemyPathGizmos();
        }

        /// <summary>
        /// Draws a coloured square for every cell (filled + wire border).
        /// When called outside Play mode the grid array is null, so every
        /// cell is rendered as Buildable to give a layout preview.
        /// </summary>
        private void DrawGridGizmos()
        {
            Vector3 cellExtents = new Vector3(cellSize * 0.94f, cellSize * 0.94f, 0.01f);
            Vector3 cellBorder = new Vector3(cellSize, cellSize, 0.01f);

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector3 center = GridToWorldPosition(x, y);

                    // Determine cell colour: use live data in Play mode, default preview outside.
                    GridCellType type = (_grid != null && Application.isPlaying)
                        ? _grid[y * gridWidth + x]
                        : GridCellType.Buildable;

                    // Filled background
                    Gizmos.color = CellGizmoColor(type);
                    Gizmos.DrawCube(center, cellExtents);

                    // Wire border
                    Gizmos.color = GizmoColorGridLine;
                    Gizmos.DrawWireCube(center, cellBorder);
                }
            }
        }

        /// <summary>
        /// Draws lines between consecutive waypoints and a sphere at each waypoint.
        /// </summary>
        private void DrawEnemyPathGizmos()
        {
            if (enemyPathWaypoints == null || enemyPathWaypoints.Length == 0) return;

            float sphereRadius = cellSize * 0.25f;

            // Segment lines
            Gizmos.color = GizmoColorPathLine;
            for (int i = 0; i < enemyPathWaypoints.Length - 1; i++)
                Gizmos.DrawLine(enemyPathWaypoints[i], enemyPathWaypoints[i + 1]);

            // Waypoint markers
            Gizmos.color = GizmoColorWaypoint;
            foreach (Vector3 wp in enemyPathWaypoints)
                Gizmos.DrawSphere(wp, sphereRadius);
        }

        /// <summary>Returns the Gizmo fill colour for a given <see cref="GridCellType"/>.</summary>
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
