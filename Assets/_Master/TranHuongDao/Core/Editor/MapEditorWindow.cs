using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Abel.TranHuongDao.Core.Editor
{
    public class MapEditorWindow : EditorWindow
    {
        // ── Enums ─────────────────────────────────────────────────────────────────
        private enum EditMode { Settings, EditPaths, PaintObstacles }

        // ── State ─────────────────────────────────────────────────────────────────
        private Vector2           leftScrollPos;
        private Vector2           rightScrollPos;
        private List<MapConfigSO> mapConfigs  = new List<MapConfigSO>();
        private MapConfigSO       activeMap;
        private EditMode          currentMode;

        // ── Layout constants ──────────────────────────────────────────────────────
        private const float LeftPaneWidth  = 200f;
        private const float PaneDividerGap = 6f;

        // ── Grid visual style ─────────────────────────────────────────────────────
        private static readonly Color GridLineColor    = new Color(0.4f, 0.8f, 1f, 0.35f);
        private static readonly Color GridBorderColor  = new Color(0.4f, 0.8f, 1f, 0.90f);
        private static readonly Color GridOriginColor  = new Color(1.0f, 0.8f, 0.2f, 0.90f);

        // ── Path visual style ─────────────────────────────────────────────────────
        // Each path in EnemyPaths gets a distinct color; index wraps around the palette.
        private static readonly Color[] PathColors =
        {
            new Color(1.00f, 0.40f, 0.40f, 1f), // red
            new Color(0.40f, 1.00f, 0.40f, 1f), // green
            new Color(1.00f, 1.00f, 0.20f, 1f), // yellow
            new Color(0.70f, 0.40f, 1.00f, 1f), // purple
            new Color(1.00f, 0.60f, 0.20f, 1f), // orange
            new Color(0.20f, 0.85f, 1.00f, 1f), // cyan
        };
        private const float PathLineWidth     = 3f;   // screen-space pixels for AAPolyLine
        private const float WaypointDiscSize  = 0.18f; // fraction of cell size for disc radius

        // ── Path editor state ─────────────────────────────────────────────────────
        // Index of the path whose foldout is currently open in the GUI (-1 = none).
        private int selectedPathIndex = -1;

        // ── Menu item ─────────────────────────────────────────────────────────────
        [MenuItem("Abel/TranHuongDao Map Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<MapEditorWindow>("Map Editor");
            window.minSize = new Vector2(600f, 400f);
            window.Show();
        }

        // ── Unity callbacks ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            LoadAllMapConfigs();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            {
                DrawLeftPane();

                // Thin visual divider between the two panes.
                GUILayout.Box(GUIContent.none,
                    GUILayout.Width(PaneDividerGap),
                    GUILayout.ExpandHeight(true));

                DrawRightPane();
            }
            EditorGUILayout.EndHorizontal();

            // Whenever any value in the window changes, refresh the Scene View
            // so the grid overlay stays in sync without requiring a mouse move.
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        // ── Pane drawing ──────────────────────────────────────────────────────────

        /// <summary>
        /// Renders the left pane: a scrollable list of discovered MapConfigSO assets
        /// and a Refresh button at the bottom.
        /// </summary>
        private void DrawLeftPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftPaneWidth));
            {
                EditorGUILayout.LabelField("Map Configs", EditorStyles.boldLabel);
                EditorGUILayout.Space(2f);

                leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUILayout.ExpandHeight(true));
                {
                    foreach (var map in mapConfigs)
                    {
                        if (map == null) continue;

                        bool isSelected = map == activeMap;

                        // Highlight the active map with a bold style.
                        var style = isSelected ? EditorStyles.boldLabel : EditorStyles.miniButton;

                        if (GUILayout.Button(map.name, EditorStyles.miniButton))
                        {
                            SelectMap(map);
                        }
                    }

                    if (mapConfigs.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No MapConfigSO found in project.", MessageType.Info);
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(4f);

                if (GUILayout.Button("Refresh List", GUILayout.Height(24f)))
                {
                    LoadAllMapConfigs();
                }
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Renders the right pane: shows a toolbar to switch edit modes and delegates
        /// to the corresponding draw method.
        /// </summary>
        private void DrawRightPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            {
                if (activeMap == null)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select a map", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    // ── Toolbar ───────────────────────────────────────────────────
                    EditorGUILayout.Space(4f);
                    int selected = GUILayout.Toolbar(
                        (int)currentMode,
                        new string[] { "Settings", "Paths", "Buildable Cells" });

                    if (selected != (int)currentMode)
                    {
                        currentMode = (EditMode)selected;
                        GUI.FocusControl(null); // Clear keyboard focus on tab switch.
                    }

                    EditorGUILayout.Space(6f);

                    // ── Mode content ──────────────────────────────────────────────
                    rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos, GUILayout.ExpandHeight(true));
                    {
                        switch (currentMode)
                        {
                            case EditMode.Settings:       DrawSettings();  break;
                            case EditMode.EditPaths:      DrawPaths();     break;
                            case EditMode.PaintObstacles: DrawPaint();     break;
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();
        }

        // ── Mode panels (to be implemented) ──────────────────────────────────────

        /// <summary>
        /// Draws the Settings panel: allows editing of the core grid properties
        /// (MapID, dimensions, cell size, origin) with full Undo support.
        /// </summary>
        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            Undo.RecordObject(activeMap, "Edit Map Settings");

            EditorGUI.BeginChangeCheck();

            // ── Identification ────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Identification", EditorStyles.miniBoldLabel);
            activeMap.MapID = EditorGUILayout.TextField("Map ID", activeMap.MapID);

            EditorGUILayout.Space(6f);

            // ── Grid dimensions ───────────────────────────────────────────────────
            EditorGUILayout.LabelField("Grid Dimensions", EditorStyles.miniBoldLabel);

            activeMap.GridWidth = Mathf.Max(1,
                EditorGUILayout.IntField(new GUIContent("Width (columns)",
                    "Number of cells along the X axis."),
                    activeMap.GridWidth));

            activeMap.GridHeight = Mathf.Max(1,
                EditorGUILayout.IntField(new GUIContent("Height (rows)",
                    "Number of cells along the Z axis."),
                    activeMap.GridHeight));

            activeMap.CellSize = Mathf.Max(0.01f,
                EditorGUILayout.FloatField(new GUIContent("Cell Size",
                    "World-unit size of one grid cell."),
                    activeMap.CellSize));

            EditorGUILayout.Space(6f);

            // ── Origin ────────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("World Origin", EditorStyles.miniBoldLabel);
            activeMap.OriginPosition = EditorGUILayout.Vector3Field(
                new GUIContent("Origin Position",
                    "World-space anchor for the bottom-left corner of the grid.\n" +
                    "X/Z define the 2D logical position; Y is the world height plane."),
                activeMap.OriginPosition);

            if (EditorGUI.EndChangeCheck())
            {
                // Mark the ScriptableObject dirty so Unity saves the changes.
                EditorUtility.SetDirty(activeMap);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(8f);

            // ── Read-only summary ─────────────────────────────────────────────────
            float totalW = activeMap.GridWidth  * activeMap.CellSize;
            float totalH = activeMap.GridHeight * activeMap.CellSize;
            EditorGUILayout.HelpBox(
                $"Grid footprint: {totalW:F2} × {totalH:F2} world units  ({activeMap.GridWidth * activeMap.GridHeight} cells total)",
                MessageType.None);
        }

        /// <summary>
        /// Draws the Path editing panel: list of paths with per-waypoint controls
        /// and a bulk snap-to-grid action.
        /// </summary>
        private void DrawPaths()
        {
            EditorGUILayout.LabelField("Enemy Paths", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            var paths = activeMap.EnemyPaths;

            // ── Add New Path ──────────────────────────────────────────────────────
            if (GUILayout.Button("＋  Add New Path", GUILayout.Height(24f)))
            {
                Undo.RecordObject(activeMap, "Add Enemy Path");
                paths.Add(new PathData());
                selectedPathIndex = paths.Count - 1; // auto-expand the new path
                EditorUtility.SetDirty(activeMap);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(6f);

            // ── Per-path blocks ───────────────────────────────────────────────────
            int removeAt = -1; // deferred removal to avoid modifying list mid-loop

            for (int pi = 0; pi < paths.Count; pi++)
            {
                var   pathData  = paths[pi];
                Color pathColor = PathColors[pi % PathColors.Length];

                // Colored label strip acting as a foldout header.
                EditorGUILayout.BeginHorizontal();
                {
                    // Color swatch
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = pathColor;
                    GUILayout.Box(GUIContent.none, GUILayout.Width(12f), GUILayout.Height(18f));
                    GUI.backgroundColor = prevBg;

                    // Foldout toggle
                    bool isOpen = selectedPathIndex == pi;
                    bool toggle = EditorGUILayout.Foldout(isOpen,
                        $"Path {pi}  ({pathData.waypoints.Count} waypoints)",
                        true, EditorStyles.foldoutHeader);
                    if (toggle != isOpen)
                        selectedPathIndex = toggle ? pi : -1;

                    // Remove button at the right edge
                    GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                    if (GUILayout.Button("✕", GUILayout.Width(22f), GUILayout.Height(18f)))
                        removeAt = pi;
                    GUI.backgroundColor = prevBg;
                }
                EditorGUILayout.EndHorizontal();

                if (selectedPathIndex != pi) continue; // collapsed — skip content

                EditorGUI.indentLevel++;

                // ── Waypoint list ─────────────────────────────────────────────────
                int removeWpAt = -1;

                for (int wi = 0; wi < pathData.waypoints.Count; wi++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField($"WP {wi}", GUILayout.Width(38f));

                        EditorGUI.BeginChangeCheck();
                        Vector3 newPos = EditorGUILayout.Vector3Field(
                            GUIContent.none, pathData.waypoints[wi]);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(activeMap, "Move Waypoint");
                            pathData.waypoints[wi] = newPos;
                            EditorUtility.SetDirty(activeMap);
                            SceneView.RepaintAll();
                        }

                        // Inline delete button
                        var prevBg = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                        if (GUILayout.Button("✕", GUILayout.Width(22f)))
                            removeWpAt = wi;
                        GUI.backgroundColor = prevBg;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // Deferred waypoint removal
                if (removeWpAt >= 0)
                {
                    Undo.RecordObject(activeMap, "Remove Waypoint");
                    pathData.waypoints.RemoveAt(removeWpAt);
                    EditorUtility.SetDirty(activeMap);
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space(2f);

                // ── Add Waypoint ──────────────────────────────────────────────────
                if (GUILayout.Button("＋  Add Waypoint"))
                {
                    Undo.RecordObject(activeMap, "Add Waypoint");

                    // Place the new waypoint one cell ahead of the last one (or at origin).
                    Vector3 spawnPos = pathData.waypoints.Count > 0
                        ? pathData.waypoints[pathData.waypoints.Count - 1]
                          + new Vector3(activeMap.CellSize, 0f, 0f)
                        : activeMap.OriginPosition;

                    pathData.waypoints.Add(spawnPos);
                    EditorUtility.SetDirty(activeMap);
                    SceneView.RepaintAll();
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4f);
            }

            // Deferred path removal (outside the loop to avoid invalidating indices)
            if (removeAt >= 0)
            {
                Undo.RecordObject(activeMap, "Remove Enemy Path");
                paths.RemoveAt(removeAt);
                if (selectedPathIndex >= paths.Count)
                    selectedPathIndex = paths.Count - 1;
                EditorUtility.SetDirty(activeMap);
                SceneView.RepaintAll();
            }

            // ── Snap All Waypoints To Grid ────────────────────────────────────────
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // divider rule

            if (GUILayout.Button("Snap All Waypoints To Grid", GUILayout.Height(24f)))
            {
                Undo.RecordObject(activeMap, "Snap All Waypoints To Grid");
                foreach (var pd in paths)
                    for (int wi = 0; wi < pd.waypoints.Count; wi++)
                        pd.waypoints[wi] = SnapToGrid(pd.waypoints[wi]);
                EditorUtility.SetDirty(activeMap);
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Draws the Paint Buildable Cells panel with usage instructions and a clear button.
        /// </summary>
        private void DrawPaint()
        {
            EditorGUILayout.LabelField("Paint Buildable Cells", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            // ── Usage instructions ──────────────────────────────────────────────────
            EditorGUILayout.HelpBox(
                "Left Click & Drag — Mark cell as Buildable\n" +
                "Shift + Left Click & Drag — Remove Buildable mark\n" +
                "Click 'Clear All' to reset every buildable cell.",
                MessageType.Info);

            EditorGUILayout.Space(4f);

            // ── Stats ────────────────────────────────────────────────────────────
            int buildableCount = activeMap.BuildableCells?.Count ?? 0;
            EditorGUILayout.LabelField($"Buildable cells: {buildableCount}",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(6f);

            // ── Clear All button ──────────────────────────────────────────────────
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("Clear All Buildable Cells", GUILayout.Height(26f)))
            {
                Undo.RecordObject(activeMap, "Clear All Buildable Cells");
                activeMap.BuildableCells.Clear();
                EditorUtility.SetDirty(activeMap);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = prevBg;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans the AssetDatabase for every MapConfigSO in the project
        /// and populates <see cref="mapConfigs"/>.
        /// </summary>
        private void LoadAllMapConfigs()
        {
            mapConfigs.Clear();

            string[] guids = AssetDatabase.FindAssets("t:MapConfigSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var    asset = AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (asset != null)
                    mapConfigs.Add(asset);
            }

            // If the previously active map was removed, clear the selection.
            if (activeMap != null && !mapConfigs.Contains(activeMap))
                activeMap = null;
        }

        // ── Scene View overlay ────────────────────────────────────────────────────

        /// <summary>
        /// Called every Scene View repaint.
        /// 1. Always draws the grid overlay.
        /// 2. Buildable-cell green quads are shown in both Paths and Buildable Cells modes
        ///    so designers see the full picture while editing paths.
        /// 3. Path handles are shown in both Paths and Buildable Cells modes
        ///    so designers can verify alignment while painting.
        /// 4. Mouse brush input is only active in Buildable Cells mode.
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (activeMap == null) return;

            DrawGridOverlay();

            // Show buildable cells overlay in Paths and Buildable Cells modes.
            if (currentMode == EditMode.EditPaths || currentMode == EditMode.PaintObstacles)
                DrawBuildableCells();

            // Show path handles in Paths and Buildable Cells modes.
            if (currentMode == EditMode.EditPaths || currentMode == EditMode.PaintObstacles)
                DrawPathHandles();

            // Mouse brush is only active in Buildable Cells mode.
            if (currentMode == EditMode.PaintObstacles)
                HandlePaintInput();
        }

        /// <summary>
        /// Draws the grid on the XY plane at fixed Z = OriginPosition.z
        /// (always visible regardless of edit mode).
        ///
        /// Coordinate convention — matches MapLayoutManager exactly:
        ///   • Grid column  (x)  →  World X
        ///   • Grid row     (y)  →  World Y
        ///   • Depth              →  World Z = OriginPosition.z  (fixed)
        /// </summary>
        private void DrawGridOverlay()
        {
            int     cols   = activeMap.GridWidth;
            int     rows   = activeMap.GridHeight;
            float   cell   = activeMap.CellSize;
            Vector3 origin = activeMap.OriginPosition;

            float totalX = cols * cell;
            float totalY = rows * cell;
            float z      = origin.z; // fixed world-Z depth plane

            // ── Interior grid lines ───────────────────────────────────────────────
            Handles.color = GridLineColor;

            // Vertical lines (parallel to Y axis): one per column interior
            for (int c = 1; c < cols; c++)
            {
                float wx = origin.x + c * cell;
                Handles.DrawLine(
                    new Vector3(wx, origin.y,          z),
                    new Vector3(wx, origin.y + totalY, z));
            }

            // Horizontal lines (parallel to X axis): one per row interior
            for (int r = 1; r < rows; r++)
            {
                float wy = origin.y + r * cell;
                Handles.DrawLine(
                    new Vector3(origin.x,          wy, z),
                    new Vector3(origin.x + totalX, wy, z));
            }

            // ── Border outline (brighter) ─────────────────────────────────────────
            Handles.color = GridBorderColor;

            Vector3 bl = new Vector3(origin.x,          origin.y,          z); // bottom-left
            Vector3 br = new Vector3(origin.x + totalX, origin.y,          z); // bottom-right
            Vector3 tr = new Vector3(origin.x + totalX, origin.y + totalY, z); // top-right
            Vector3 tl = new Vector3(origin.x,          origin.y + totalY, z); // top-left

            Handles.DrawLine(bl, br); // bottom edge
            Handles.DrawLine(br, tr); // right edge
            Handles.DrawLine(tr, tl); // top edge
            Handles.DrawLine(tl, bl); // left edge

            // ── Origin marker: small cross at the bottom-left anchor ───────────────────
            Handles.color = GridOriginColor;
            float crossSize = cell * 0.2f;
            Handles.DrawLine(
                origin + new Vector3(-crossSize, 0f, 0f),
                origin + new Vector3( crossSize, 0f, 0f));
            Handles.DrawLine(
                origin + new Vector3(0f, -crossSize, 0f),
                origin + new Vector3(0f,  crossSize, 0f));
        }

        /// <summary>
        /// Draws draggable PositionHandles for every waypoint in every path,
        /// connects sequential waypoints with a thick Anti-Aliased line,
        /// and labels each waypoint with its index.
        /// All modifications are wrapped in Undo and mark the asset dirty.
        ///
        /// Coordinate convention: waypoints live in world XY at fixed Z = OriginPosition.z,
        /// matching MapLayoutManager.GridToWorldPosition.
        /// </summary>
        private void DrawPathHandles()
        {
            var   paths    = activeMap.EnemyPaths;
            float cell     = activeMap.CellSize;
            float discSize = cell * WaypointDiscSize;
            float z        = activeMap.OriginPosition.z; // fixed depth plane

            for (int pi = 0; pi < paths.Count; pi++)
            {
                var   pathData  = paths[pi];
                Color pathColor = PathColors[pi % PathColors.Length];
                var   waypoints = pathData.waypoints;

                if (waypoints.Count == 0) continue;

                // ── Connecting lines ──────────────────────────────────────────────
                // Build a point array for DrawAAPolyLine (thicker than DrawLine).
                var linePoints = new Vector3[waypoints.Count];
                for (int wi = 0; wi < waypoints.Count; wi++)
                    // Lock Z to the grid's depth plane so lines stay flat.
                    linePoints[wi] = new Vector3(waypoints[wi].x, waypoints[wi].y, z);

                Handles.color = pathColor;
                if (linePoints.Length >= 2)
                    Handles.DrawAAPolyLine(PathLineWidth, linePoints);

                // ── Waypoint handles & labels ─────────────────────────────────────
                for (int wi = 0; wi < waypoints.Count; wi++)
                {
                    Vector3 worldPos = new Vector3(waypoints[wi].x, waypoints[wi].y, z);

                    // Filled disc facing the camera (normal = Vector3.back for XY plane).
                    Handles.color = pathColor;
                    Handles.DrawSolidDisc(worldPos, Vector3.back, discSize);

                    // Waypoint index label offset to the right of the disc.
                    Handles.Label(
                        worldPos + new Vector3(discSize * 1.4f, discSize * 0.5f, 0f),
                        $"P{pi}:W{wi}",
                        EditorStyles.miniLabel);

                    // PositionHandle: X and Y are meaningful; Z is locked to the grid plane.
                    EditorGUI.BeginChangeCheck();
                    Handles.color = Color.white;
                    Vector3 newWorld = Handles.PositionHandle(worldPos, Quaternion.identity);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(activeMap, "Move Waypoint");
                        // Write back X/Y only; keep Z pinned to the grid's depth plane.
                        waypoints[wi] = new Vector3(newWorld.x, newWorld.y, z);
                        EditorUtility.SetDirty(activeMap);
                        Repaint(); // Sync the Vector3 fields in the GUI panel.
                    }
                }
            }
        }

        // ── Paint helpers ─────────────────────────────────────────────────────────

        // Colors for the buildable-cell overlay (green = tower can be placed here).
        private static readonly Color BuildableFaceColor    = new Color(0.20f, 1f, 0.30f, 0.22f);
        private static readonly Color BuildableOutlineColor = new Color(0.15f, 0.90f, 0.25f, 0.85f);

        /// <summary>
        /// Renders every buildable cell as a green semi-transparent quad on the XY grid plane.
        /// Uses the same cell-corner math as MapLayoutManager.GridToWorldPosition so
        /// painted squares are perfectly pixel-aligned with the grid overlay.
        /// </summary>
        private void DrawBuildableCells()
        {
            if (activeMap.BuildableCells == null || activeMap.BuildableCells.Count == 0) return;

            float   cell   = activeMap.CellSize;
            Vector3 origin = activeMap.OriginPosition;
            float   z      = origin.z; // fixed depth plane

            // Reuse one array to avoid per-cell allocation.
            var corners = new Vector3[4];

            foreach (var cell2d in activeMap.BuildableCells)
            {
                // Compute the four corners of this grid cell in world XY space.
                //   MapLayoutManager.GridToWorldPosition uses (cx+0.5)*cell for the center;
                //   here we need the four corners, so we use cx*cell and (cx+1)*cell.
                float x0 = origin.x + cell2d.x       * cell;
                float x1 = origin.x + (cell2d.x + 1) * cell;
                float y0 = origin.y + cell2d.y       * cell;
                float y1 = origin.y + (cell2d.y + 1) * cell;

                // Unity's DrawSolidRectangleWithOutline expects corners in this winding order:
                // [0] = bottom-left, [1] = top-left, [2] = top-right, [3] = bottom-right
                corners[0] = new Vector3(x0, y0, z);
                corners[1] = new Vector3(x0, y1, z);
                corners[2] = new Vector3(x1, y1, z);
                corners[3] = new Vector3(x1, y0, z);

                Handles.DrawSolidRectangleWithOutline(corners, BuildableFaceColor, BuildableOutlineColor);
            }
        }

        /// <summary>
        /// Intercepts mouse events in the Scene View to paint or erase blocked cells.
        ///
        /// Brush logic:
        ///   • Left-click / drag         → add cell to BuildableCells
        ///   • Shift + left-click / drag → remove cell from BuildableCells
        ///
        /// World-to-grid math mirrors MapLayoutManager.WorldToGridPosition exactly:
        ///   gridX = Floor((hitX - origin.x) / cellSize)
        ///   gridY = Floor((hitY - origin.y) / cellSize)
        /// The ray is cast onto the plane z = OriginPosition.z (forward-normal plane).
        /// </summary>
        private void HandlePaintInput()
        {
            // Prevent Unity from de-selecting the active GameObject when the user clicks
            // in the Scene View while this window owns the mouse.
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            Event e = Event.current;

            // Only act on left-mouse button down or drag events.
            bool isPaint = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                           && e.button == 0;
            if (!isPaint) return;

            // ── Ray → XY plane intersection ──────────────────────────────────────────
            // The grid lives at z = OriginPosition.z. Define a forward-normal plane there.
            // Plane(normal, d): dot(point, normal) + d = 0  ⇒  z + d = 0  ⇒  d = -origin.z
            var   plane = new Plane(Vector3.forward, -activeMap.OriginPosition.z);
            Ray   ray   = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (!plane.Raycast(ray, out float enterDist)) return; // ray is parallel to the plane

            Vector3 hitPoint = ray.GetPoint(enterDist);

            // ── Convert world XY hit to grid cell ───────────────────────────────────────
            // Identical to MapLayoutManager.WorldToGridPosition (no clamping here so we
            // can reject out-of-bounds cells rather than silently clamping them).
            float   cell   = activeMap.CellSize;
            Vector3 origin = activeMap.OriginPosition;

            int gx = Mathf.FloorToInt((hitPoint.x - origin.x) / cell);
            int gy = Mathf.FloorToInt((hitPoint.y - origin.y) / cell);

            // Reject clicks outside the grid bounds.
            if (gx < 0 || gx >= activeMap.GridWidth || gy < 0 || gy >= activeMap.GridHeight)
                return;

            var  cellCoord = new Vector2Int(gx, gy);
            bool erase     = e.shift;

            // ── Apply paint / erase ───────────────────────────────────────────────
            bool listContains = activeMap.BuildableCells.Contains(cellCoord);

            if (erase && listContains)
            {
                Undo.RecordObject(activeMap, "Erase Buildable Cell");
                activeMap.BuildableCells.Remove(cellCoord);
                EditorUtility.SetDirty(activeMap);
                Repaint();
            }
            else if (!erase && !listContains)
            {
                Undo.RecordObject(activeMap, "Paint Buildable Cell");
                activeMap.BuildableCells.Add(cellCoord);
                EditorUtility.SetDirty(activeMap);
                Repaint();
            }

            // Consume the event so Unity doesn't pass it to the default scene tools.
            e.Use();
        }

        /// <summary>Sets <paramref name="map"/> as the active map and pings it in the Project window.</summary>
        private void SelectMap(MapConfigSO map)
        {
            activeMap         = map;
            currentMode       = EditMode.Settings;
            selectedPathIndex = -1;
            EditorGUIUtility.PingObject(map);
            Repaint();
        }

        /// <summary>
        /// Snaps a world position to the nearest grid cell corner on the XY plane.
        /// Z is always pinned to OriginPosition.z (the grid's depth plane).
        /// Mirrors the inverse of MapLayoutManager.GridToWorldPosition.
        /// </summary>
        private Vector3 SnapToGrid(Vector3 world)
        {
            float   cell   = activeMap.CellSize;
            Vector3 origin = activeMap.OriginPosition;
            float   x      = Mathf.Round((world.x - origin.x) / cell) * cell + origin.x;
            float   y      = Mathf.Round((world.y - origin.y) / cell) * cell + origin.y;
            return new Vector3(x, y, origin.z);
        }
    }
}
