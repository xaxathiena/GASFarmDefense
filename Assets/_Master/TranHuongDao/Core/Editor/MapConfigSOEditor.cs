using UnityEngine;
using UnityEditor;

namespace Abel.TranHuongDao.Core.Editor
{
    /// <summary>
    /// Custom Inspector and Scene View editor for MapConfigSO.
    /// Provides visual tools for designers to naturally edit grid layouts and multi-lane paths.
    /// </summary>
    [CustomEditor(typeof(MapConfigSO))]
    public class MapConfigSOEditor : UnityEditor.Editor
    {
        private MapConfigSO _config;

        private void OnEnable()
        {
            _config = (MapConfigSO)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default properties
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

            // Button to snap all drawn waypoints to the exact grid cell centers
            if (GUILayout.Button("Snap ALL Waypoints to Grid", GUILayout.Height(30)))
            {
                Undo.RecordObject(_config, "Snap Waypoints");
                SnapAllWaypointsToGrid();
                EditorUtility.SetDirty(_config);
            }

            // Button to explicitly wipe the BuildableCells whitelist
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear All Buildable Cells", GUILayout.Height(25)))
            {
                Undo.RecordObject(_config, "Clear Buildable Cells");
                _config.BuildableCells.Clear();
                EditorUtility.SetDirty(_config);
            }
            GUI.backgroundColor = Color.white;
        }

        private void OnSceneGUI()
        {
            if (_config == null) return;

            DrawGrid();
            DrawEnemyPaths();
        }

        /// <summary>
        /// Draws a visual representation of the grid in Scene View based on MapConfigSO sizes.
        /// </summary>
        private void DrawGrid()
        {
            if (_config.GridWidth <= 0 || _config.GridHeight <= 0 || _config.CellSize <= 0) return;

            Handles.color = new Color(1f, 1f, 1f, 0.15f);

            Vector3 origin = _config.OriginPosition;
            float widthDist = _config.GridWidth * _config.CellSize;
            float heightDist = _config.GridHeight * _config.CellSize;

            // Draw vertical columns
            for (int x = 0; x <= _config.GridWidth; x++)
            {
                Vector3 start = origin + new Vector3(x * _config.CellSize, 0, 0);
                Vector3 end = start + new Vector3(0, heightDist, 0);
                Handles.DrawLine(start, end);
            }

            // Draw horizontal rows
            for (int y = 0; y <= _config.GridHeight; y++)
            {
                Vector3 start = origin + new Vector3(0, y * _config.CellSize, 0);
                Vector3 end = start + new Vector3(widthDist, 0, 0);
                Handles.DrawLine(start, end);
            }
        }

        /// <summary>
        /// Iterates over all configured paths, rendering uniquely colored Handles 
        /// and connections for game designers to interact with.
        /// </summary>
        private void DrawEnemyPaths()
        {
            if (_config.EnemyPaths == null || _config.EnemyPaths.Count == 0) return;

            // Define distinct high-contrast colors for path differentiation
            Color[] pathColors = new Color[]
            {
                new Color(1f, 0.6f, 0f),   // Orange
                Color.cyan,                // Cyan
                Color.magenta,             // Magenta
                Color.green,               // Green
                Color.yellow,              // Yellow
                Color.red                  // Red
            };

            for (int i = 0; i < _config.EnemyPaths.Count; i++)
            {
                var pathData = _config.EnemyPaths[i];
                if (pathData == null || pathData.waypoints == null || pathData.waypoints.Count == 0) continue;

                Color pathColor = pathColors[i % pathColors.Length];
                Handles.color = pathColor;

                // Draw connecting segments
                for (int w = 0; w < pathData.waypoints.Count - 1; w++)
                {
                    Handles.DrawLine(pathData.waypoints[w], pathData.waypoints[w + 1], 3f);
                }

                // Draw interactive Position Handles and start label
                for (int w = 0; w < pathData.waypoints.Count; w++)
                {
                    Vector3 currentPos = pathData.waypoints[w];

                    if (w == 0)
                    {
                        // Tag the very first waypoint of the path to clarify direction
                        GUIStyle labelStyle = new GUIStyle();
                        labelStyle.normal.textColor = pathColor;
                        labelStyle.fontStyle = FontStyle.Bold;
                        Handles.Label(currentPos + new Vector3(0, 0.5f, 0), $"Path {i} Start", labelStyle);
                    }

                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.PositionHandle(currentPos, Quaternion.identity);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_config, "Move Waypoint");
                        pathData.waypoints[w] = newPos;
                        EditorUtility.SetDirty(_config);
                    }

                    // Render simple dotted cap layout
                    Handles.SphereHandleCap(
                        0, newPos, Quaternion.identity, _config.CellSize * 0.2f, EventType.Repaint);
                }
            }
        }

        /// <summary>
        /// Mathematically aligns every single path waypoint into the exact geometrical center 
        /// of whatever grid cell it happens to occupy.
        /// </summary>
        private void SnapAllWaypointsToGrid()
        {
            if (_config.EnemyPaths == null || _config.CellSize <= 0) return;

            foreach (var path in _config.EnemyPaths)
            {
                if (path == null || path.waypoints == null) continue;

                for (int w = 0; w < path.waypoints.Count; w++)
                {
                    Vector3 currentPos = path.waypoints[w];

                    // Convert raw space into grid indices
                    int xIndex = Mathf.FloorToInt((currentPos.x - _config.OriginPosition.x) / _config.CellSize);
                    int yIndex = Mathf.FloorToInt((currentPos.y - _config.OriginPosition.y) / _config.CellSize);

                    // Clamp to make sure they aren't shot off-grid entirely
                    xIndex = Mathf.Clamp(xIndex, 0, Mathf.Max(0, _config.GridWidth - 1));
                    yIndex = Mathf.Clamp(yIndex, 0, Mathf.Max(0, _config.GridHeight - 1));

                    // Back calculate exact geometrical center
                    Vector3 snappedPos = new Vector3(
                        _config.OriginPosition.x + (xIndex + 0.5f) * _config.CellSize,
                        _config.OriginPosition.y + (yIndex + 0.5f) * _config.CellSize,
                        _config.OriginPosition.z
                    );

                    path.waypoints[w] = snappedPos;
                }
            }
        }
    }
}
