using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GAS;
using Abel.TranHuongDao.Core;

namespace Abel.TranHuongDao.EditorTools
{
    /// <summary>
    /// Custom inspector for AbilitiesConfig.
    /// Displays a searchable, scrollable list of all registered abilities.
    /// </summary>
    [CustomEditor(typeof(AbilitiesConfig))]
    public class AbilitiesConfigEditor : Editor
    {
        // ── State ────────────────────────────────────────────────────────────
        private string searchText = "";
        private Vector2 scrollPosition;

        // ── Cached styles (created once to avoid GC per frame) ───────────────
        private GUIStyle _headerStyle;

        // ── Layout constants ─────────────────────────────────────────────────
        private const float ROW_HEIGHT   = 22f;   // compact single-line row
        private const float SCROLL_MAX_H = 420f;

        // ────────────────────────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            // Keep the SO data in sync before drawing
            serializedObject.Update();

            AbilitiesConfig config = (AbilitiesConfig)target;

            InitStyles();

            // ── 1. Header ────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Ability Registry", _headerStyle);
            EditorGUILayout.Space(4);

            // ── 2. Search bar row ────────────────────────────────────────────
            DrawSearchBar();

            EditorGUILayout.Space(6);

            // ── 3. Build filtered list ───────────────────────────────────────
            List<GameplayAbilityData> filtered = BuildFilteredList(config.allAbilities);

            // ── 4. Count label ───────────────────────────────────────────────
            string countLabel = string.IsNullOrEmpty(searchText)
                ? $"Total: {config.allAbilities.Count} abilities"
                : $"Showing {filtered.Count} / {config.allAbilities.Count} abilities";

            EditorGUILayout.LabelField(countLabel, EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            // ── 5. Scrollable results ────────────────────────────────────────
            float scrollHeight = Mathf.Min(filtered.Count * (ROW_HEIGHT + 6f) + 10f, SCROLL_MAX_H);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scrollHeight));
            {
                DrawAbilityList(filtered);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            // ── 6. Expose only the allAbilities array for drag-and-drop management.
            //    Using SerializedProperty keeps undo/redo and the reorderable list intact
            //    without exposing unrelated SO fields via DrawDefaultInspector().
            EditorGUILayout.LabelField("Ability List", EditorStyles.boldLabel);
            SerializedProperty abilitiesProp = serializedObject.FindProperty("allAbilities");
            EditorGUILayout.PropertyField(abilitiesProp, true);

            serializedObject.ApplyModifiedProperties();
        }

        // ────────────────────────────────────────────────────────────────────
        // Search Bar
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders the search field with a Clear button on the same row.
        /// Uses the built-in "SearchTextField" skin when available; falls back
        /// to a standard text field so it compiles on all Unity versions.
        /// </summary>
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                // Try the "SearchTextField" style; fall back gracefully
                GUIStyle searchStyle = GUI.skin.FindStyle("SearchTextField") ?? EditorStyles.textField;

                EditorGUILayout.LabelField("Search", GUILayout.Width(48));
                searchText = EditorGUILayout.TextField(searchText, searchStyle);

                // Clear button — only show when there is text to clear
                if (!string.IsNullOrEmpty(searchText))
                {
                    if (GUILayout.Button("✕ Clear", GUILayout.Width(64)))
                    {
                        searchText = "";
                        GUI.FocusControl(null); // Dismiss keyboard focus so the field updates immediately
                    }
                }
                else
                {
                    // Reserve the same space so the layout does not shift
                    GUILayout.Space(68);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ────────────────────────────────────────────────────────────────────
        // Filter logic
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all abilities that match the current <see cref="searchText"/>.
        /// Matching is case-insensitive and checks Unity's asset name (SO.name).
        /// When searchText is empty, the full list is returned as-is.
        /// </summary>
        private List<GameplayAbilityData> BuildFilteredList(List<GameplayAbilityData> source)
        {
            if (string.IsNullOrEmpty(searchText))
                return source;

            string query = searchText.ToLowerInvariant();
            var results = new List<GameplayAbilityData>(source.Count);

            foreach (GameplayAbilityData ability in source)
            {
                if (ability == null) continue;

                // Match against the SO asset name (the filename Unity assigns to the asset)
                if (ability.name.ToLowerInvariant().Contains(query))
                    results.Add(ability);
            }

            return results;
        }

        // ────────────────────────────────────────────────────────────────────
        // List renderer
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws one compact row per ability inside the scroll view.
        /// Layout per row: [ObjectField (read-only)] [Ping] [Edit]
        /// </summary>
        private void DrawAbilityList(List<GameplayAbilityData> list)
        {
            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox("No abilities match the search query.", MessageType.Info);
                return;
            }

            foreach (GameplayAbilityData ability in list)
            {
                if (ability == null)
                {
                    // Null slot — warn the designer so they can clean up the list
                    EditorGUILayout.HelpBox("Missing (null) ability entry — remove it from the list.", MessageType.Warning);
                    continue;
                }

                // "box" style gives each row a subtle border, keeping rows visually distinct
                EditorGUILayout.BeginHorizontal("box");
                {
                    // Disabled ObjectField: read-only reference — designers can still click to locate the asset
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(ability, typeof(GameplayAbilityData), false);
                    }

                    // Ping — flashes the asset in the Project window without changing selection
                    if (GUILayout.Button("Ping", GUILayout.Width(42)))
                        EditorGUIUtility.PingObject(ability);

                    // Edit — opens the asset so the designer can modify its fields immediately
                    if (GUILayout.Button("Edit", GUILayout.Width(42)))
                        AssetDatabase.OpenAsset(ability);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Style helpers
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lazily initialises GUIStyles once so they persist across repaints
        /// without allocating new objects every frame.
        /// </summary>
        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin   = new RectOffset(0, 0, 4, 4)
                };
            }

        }
    }
}
