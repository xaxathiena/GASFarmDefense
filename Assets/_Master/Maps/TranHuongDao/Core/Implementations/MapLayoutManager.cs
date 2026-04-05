using System;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using UnityEngine;
using VContainer;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// DOD grid-and-path layout manager for the Tower Defense map.
    /// Uses MapConfigSO to build the grid and multiple enemy paths.
    /// </summary>
    [ExecuteAlways]
    public class MapLayoutManager : MonoBehaviour, IMapLayoutManager
    {
        // ─────────────────────────────────────────────────────────────────────────
        //  Internal DOD data
        // ─────────────────────────────────────────────────────────────────────────

        private GridCellType[] _grid;
        private IReadOnlyList<Vector3>[] _cachedPaths;      // Local space coordinates
        private IReadOnlyList<Vector3>[] _cachedWorldPaths; // World space coordinates
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
            if (Application.isPlaying && _configService != null)
            {
                LoadMap("Map_1"); // Temporary default
            }
        }

        /// <summary>
        /// Allows the editor to manually trigger a map load for Gizmo visualization.
        /// </summary>
        [ContextMenu("Reload Map (Editor Only)")]
        public void EditorLoadMap()
        {
            if (_editorPreviewConfig == null) return;
            Initialize(_editorPreviewConfig);
        }

        public void Initialize(MapConfigSO config)
        {
            if (config == null) return;

            GridWidth = config.GridWidth;
            GridHeight = config.GridHeight;
            CellSize = config.CellSize;
            OriginPosition = config.OriginPosition;
            _grid = new GridCellType[GridWidth * GridHeight];

            // Convert List<PathData> to IReadOnlyList<Vector3>[]
            if (config.EnemyPaths != null)
            {
                _cachedPaths = new IReadOnlyList<Vector3>[config.EnemyPaths.Count];
                for (int i = 0; i < config.EnemyPaths.Count; i++)
                {
                    _cachedPaths[i] = config.EnemyPaths[i].waypoints;
                }
            }
            else
            {
                _cachedPaths = Array.Empty<IReadOnlyList<Vector3>>();
            }


            _cachedWorldPaths = null; // Clear world cache

            // Mark buildable cells
            if (config.BuildableCells != null)
            {
                foreach (var cell in config.BuildableCells)
                    SetCellStateInternal(cell, GridCellType.Buildable);
            }

            // Mark path cells
            if (_cachedPaths != null)
            {
                foreach (var path in _cachedPaths)
                    MarkPathCells(path);
            }
        }

        public void LoadMap(string mapID)
        {
            if (_configService == null) return;

            var config = _configService.GetConfig<MapConfigSO>();
            if (config == null)
            {
                Debug.LogError($"[MapLayoutManager] Missing MapConfigSO in ConfigService for {mapID}.");
                return;
            }
            Initialize(config);
        }

        private void MarkPathCells(IEnumerable<Vector3> waypoints)
        {
            if (waypoints == null) return;


            Vector3? lastPoint = null;
            foreach (var point in waypoints)
            {
                SetCellStateInternal(WorldToGridPosition(transform.TransformPoint(point)), GridCellType.Path);


                if (lastPoint.HasValue)
                {
                    Vector3 from = lastPoint.Value;
                    Vector3 to = point;
                    float length = Vector3.Distance(from, to);
                    int steps = Mathf.CeilToInt(length / (CellSize * 0.5f));
                    for (int s = 1; s <= steps; s++)
                    {
                        Vector3 sample = Vector3.Lerp(from, to, (float)s / steps);
                        SetCellStateInternal(WorldToGridPosition(transform.TransformPoint(sample)), GridCellType.Path);
                    }
                }
                lastPoint = point;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  IMapLayoutManager – Coordinate conversion
        // ─────────────────────────────────────────────────────────────────────────

        public Vector3 GridToWorldPosition(int x, int y)
        {
            // Calculate local offset from OriginPosition
            Vector3 localPos = new Vector3(
                OriginPosition.x + (x + 0.5f) * CellSize,
                OriginPosition.y + (y + 0.5f) * CellSize,
                OriginPosition.z
            );

            // Map local offset into world space using the GameObject's Transform (handles Rotation/Tilt)
            return transform.TransformPoint(localPos);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            if (CellSize <= 0) return Vector2Int.zero;

            // Map world position back into local space
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);

            int x = Mathf.FloorToInt((localPos.x - OriginPosition.x) / CellSize);
            int y = Mathf.FloorToInt((localPos.y - OriginPosition.y) / CellSize);

            return new Vector2Int(
                Mathf.Clamp(x, 0, Mathf.Max(0, GridWidth - 1)),
                Mathf.Clamp(y, 0, Mathf.Max(0, GridHeight - 1))
            );
        }

        public GridCellType GetCellState(Vector2Int gridPos) => IsValidCell(gridPos) ? _grid[gridPos.y * GridWidth + gridPos.x] : GridCellType.Blocked;
        public void SetCellState(Vector2Int gridPos, GridCellType state) => SetCellStateInternal(gridPos, state);
        public bool CanBuildAt(Vector3 worldPosition) => GetCellState(WorldToGridPosition(worldPosition)) == GridCellType.Buildable;

        public IReadOnlyList<Vector3>[] GetEnemyPath()
        {
            // If the local cache is empty, return empty
            if (_cachedPaths == null) return Array.Empty<IReadOnlyList<Vector3>>();

            // Recompute world paths if needed
            if (_cachedWorldPaths == null || _cachedWorldPaths.Length != _cachedPaths.Length)
            {
                _cachedWorldPaths = new IReadOnlyList<Vector3>[_cachedPaths.Length];
                for (int i = 0; i < _cachedPaths.Length; i++)
                {
                    var localPath = _cachedPaths[i];
                    var worldPath = new Vector3[localPath.Count];
                    for (int j = 0; j < localPath.Count; j++)
                    {
                        // Transform each local waypoint into world space based on current Transform
                        worldPath[j] = transform.TransformPoint(localPath[j]);
                    }
                    _cachedWorldPaths[i] = worldPath;
                }
            }

            return _cachedWorldPaths;
        }

        private void SetCellStateInternal(Vector2Int gridPos, GridCellType state)
        {
            if (IsValidCell(gridPos)) _grid[gridPos.y * GridWidth + gridPos.x] = state;
        }
        private bool IsValidCell(Vector2Int gridPos) => gridPos.x >= 0 && gridPos.x < GridWidth && gridPos.y >= 0 && gridPos.y < GridHeight;

        // ─────────────────────────────────────────────────────────────────────────
        //  Gizmos
        // ─────────────────────────────────────────────────────────────────────────

        private static readonly Color GizmoColorBuildable = new Color(0.20f, 0.80f, 0.20f, 0.35f);
        private static readonly Color GizmoColorPath = new Color(0.90f, 0.30f, 0.30f, 0.50f);
        private static readonly Color GizmoColorBlocked = new Color(0.30f, 0.30f, 0.30f, 0.50f);
        private static readonly Color GizmoColorTowerOccupied = new Color(0.20f, 0.40f, 0.90f, 0.50f);
        private static readonly Color GizmoColorEmpty = new Color(1.00f, 1.00f, 1.00f, 0.10f);
        private static readonly Color GizmoColorGridLine = new Color(1.00f, 1.00f, 1.00f, 0.15f);
        private static readonly Color GizmoColorWaypoint = new Color(1.00f, 0.60f, 0.00f, 1.00f);
        private static readonly Color GizmoColorPathLine = new Color(1.00f, 0.40f, 0.10f, 0.90f);

        [Header("Editor Visualization")]
        [SerializeField] private MapConfigSO _editorPreviewConfig;
        [SerializeField] private bool _showGrid = true;
        [SerializeField] private bool _showPaths = true;

        private void OnDrawGizmos()
        {
            if (GridWidth <= 0 || GridHeight <= 0 || CellSize <= 0)
            {
                // Try automatic reload if config is available
                if (!Application.isPlaying && _editorPreviewConfig != null) EditorLoadMap();
                return;
            }

            if (_showGrid) DrawGridGizmos();
            if (_showPaths) DrawEnemyPathGizmos();
        }

        private void DrawGridGizmos()
        {
            Vector3 cellExtents = new Vector3(CellSize * 0.94f, CellSize * 0.94f, 0.01f);
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    Vector3 center = GridToWorldPosition(x, y);
                    GridCellType type = (_grid != null && y * GridWidth + x < _grid.Length) ? _grid[y * GridWidth + x] : GridCellType.Empty;

                    Gizmos.color = CellGizmoColor(type);
                    Gizmos.DrawCube(center, cellExtents);
                    Gizmos.color = GizmoColorGridLine;
                    Gizmos.DrawWireCube(center, new Vector3(CellSize, CellSize, 0.01f));
                }
            }
        }

        private void DrawEnemyPathGizmos()
        {
            // IMPORTANT: GetEnemyPath() now returns WORLD coordinates.
            var worldPaths = GetEnemyPath();
            if (worldPaths == null || worldPaths.Length == 0) return;


            float sphereRadius = CellSize * 0.25f;

            for (int pIdx = 0; pIdx < worldPaths.Length; pIdx++)
            {
                var path = worldPaths[pIdx];
                if (path == null) continue;

                Gizmos.color = GizmoColorPathLine;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector3 from = path[i];
                    Vector3 to = path[i + 1];
                    DrawArrow(from, to, CellSize * 0.4f);
                    Gizmos.DrawLine(from, to);
                }

                for (int i = 0; i < path.Count; i++)
                {
                    Vector3 worldPos = path[i];
                    Gizmos.color = (i == 0) ? Color.green : (i == path.Count - 1 ? Color.red : GizmoColorWaypoint);
                    Gizmos.DrawSphere(worldPos, sphereRadius);
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(worldPos + Vector3.up * sphereRadius * 2f, $"P{pIdx}:{i}");
#endif
                }
            }
        }

        private void DrawArrow(Vector3 from, Vector3 to, float arrowHeadLength)
        {
            Vector3 direction = (to - from).normalized;
            if (direction == Vector3.zero) return;
            Vector3 right = Quaternion.LookRotation(Vector3.forward, direction) * Quaternion.Euler(0, 0, 150) * Vector3.up;
            Vector3 left = Quaternion.LookRotation(Vector3.forward, direction) * Quaternion.Euler(0, 0, -150) * Vector3.up;
            Gizmos.DrawRay(to, right * arrowHeadLength);
            Gizmos.DrawRay(to, left * arrowHeadLength);
        }

        private static Color CellGizmoColor(GridCellType type) => type switch
        {
            GridCellType.Buildable => GizmoColorBuildable,
            GridCellType.Path => GizmoColorPath,
            GridCellType.Blocked => GizmoColorBlocked,
            GridCellType.TowerOccupied => GizmoColorTowerOccupied,
            _ => GizmoColorEmpty,
        };
    }
}
