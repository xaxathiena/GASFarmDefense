#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GAS;
using Abel.TranHuongDao.Core;

namespace Abel.TranHuongDao.Core.Abilities.Editor
{
    public class AbilitiesManagerWindow : EditorWindow
    {
        public enum EManagerTab
        {
            Abilities,
            Effects
        }

        // Layout constants
        private const float LEFT_PANEL_WIDTH = 300f;
        private const float ROW_HEIGHT = 24f;

        // State
        private EManagerTab _currentTab = EManagerTab.Abilities;
        private string _searchQuery = "";
        private bool _showUnassignedOnly = false;
        private Vector2 _leftScrollPos;
        private Vector2 _rightScrollPos;
        
        // Data
        private AbilitiesConfig _config;

        private List<GameplayAbilityData> _allAbilities = new List<GameplayAbilityData>();
        private List<GameplayAbilityData> _filteredAbilities = new List<GameplayAbilityData>();
        
        private List<GameplayEffect> _allEffects = new List<GameplayEffect>();
        private List<GameplayEffect> _filteredEffects = new List<GameplayEffect>();
        
        // Selection
        private UnityEngine.Object _selectedAsset;
        private UnityEditor.Editor _cachedEditor;

        // Styles
        private GUIStyle _selectedRowStyle;
        private GUIStyle _unselectedRowStyle;

        [MenuItem("GAS Farm Defense/Open Abilities Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<AbilitiesManagerWindow>("GAS Asset Manager");
            window.minSize = new Vector2(700, 450);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            RefreshAllLists();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
            }
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is GameplayEffect effect)
            {
                if (_allEffects.Contains(effect))
                {
                    _currentTab = EManagerTab.Effects;
                    _searchQuery = ""; 
                    ApplySearchFilter();
                    SelectAsset(effect);
                    Repaint();
                }
            }
            else if (Selection.activeObject is GameplayAbilityData ability)
            {
                if (_allAbilities.Contains(ability))
                {
                    _currentTab = EManagerTab.Abilities;
                    _searchQuery = "";
                    ApplySearchFilter();
                    SelectAsset(ability);
                    Repaint();
                }
            }
        }

        private void RefreshAllLists()
        {
            // Auto-locate AbilitiesConfig
            if (_config == null)
            {
                string[] configGuids = AssetDatabase.FindAssets("t:AbilitiesConfig");
                if (configGuids.Length > 0)
                {
                    _config = AssetDatabase.LoadAssetAtPath<AbilitiesConfig>(AssetDatabase.GUIDToAssetPath(configGuids[0]));
                }
            }

            // --- 1. Load Abilities ---
            _allAbilities.Clear();
            string[] abilityGuids = AssetDatabase.FindAssets("t:GameplayAbilityData");
            foreach (string guid in abilityGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilityData>(path);
                if (ability != null) _allAbilities.Add(ability);
            }
            _allAbilities = _allAbilities.OrderBy(a => a.name).ToList();

            // --- 2. Load Effects ---
            _allEffects.Clear();
            string[] effectGuids = AssetDatabase.FindAssets("t:GameplayEffect");
            foreach (string guid in effectGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var effect = AssetDatabase.LoadAssetAtPath<GameplayEffect>(path);
                if (effect != null) _allEffects.Add(effect);
            }
            _allEffects = _allEffects.OrderBy(e => e.name).ToList();

            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            // Filter Abilities
            var abQueryList = _allAbilities.AsEnumerable();
            if (_showUnassignedOnly && _config != null)
            {
                abQueryList = abQueryList.Where(a => !_config.allAbilities.Contains(a));
            }
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                string lowerQuery = _searchQuery.ToLowerInvariant();
                abQueryList = abQueryList.Where(a => a.name.ToLowerInvariant().Contains(lowerQuery) || 
                                                     (a.abilityName != null && a.abilityName.ToLowerInvariant().Contains(lowerQuery)));
            }
            _filteredAbilities = abQueryList.ToList();

            // Filter Effects (no config to check, just search by name)
            var efQueryList = _allEffects.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                string lowerQuery = _searchQuery.ToLowerInvariant();
                efQueryList = efQueryList.Where(e => e.name.ToLowerInvariant().Contains(lowerQuery));
            }
            _filteredEffects = efQueryList.ToList();
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginHorizontal();
            
            // ==========================================
            // LEFT PANEL: List and Search
            // ==========================================
            DrawLeftPanel();

            // Vertical separator line
            DrawSeparatorLine();

            // ==========================================
            // RIGHT PANEL: Specific Data Properties
            // ==========================================
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH), GUILayout.ExpandHeight(true));
            GUILayout.Space(4);

            // --- Tabs ---
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            
            // Using a large toggle button style or Toolbar tabs
            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 30, fontSize = 12, fontStyle = FontStyle.Bold };
            int selectedTab = GUILayout.Toolbar((int)_currentTab, new string[] { "Abilities", "Effects" }, tabStyle);
            
            if (EditorGUI.EndChangeCheck())
            {
                _currentTab = (EManagerTab)selectedTab;
                _searchQuery = "";
                _showUnassignedOnly = false;
                GUI.FocusControl(null);
                ApplySearchFilter();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
            
            // --- Common Toolbar (Refresh & Search) ---
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshAllLists();
            }

            GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.textField;
            GUIStyle cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? EditorStyles.miniButton;

            EditorGUI.BeginChangeCheck();
            _searchQuery = EditorGUILayout.TextField(_searchQuery, searchStyle);
            if (EditorGUI.EndChangeCheck()) ApplySearchFilter();

            if (GUILayout.Button("", cancelStyle))
            {
                _searchQuery = "";
                GUI.FocusControl(null); // Remove focus to clear text field immediately
                ApplySearchFilter();
            }
            EditorGUILayout.EndHorizontal();

            // --- Tab-Specific Toolbar & Content ---
            if (_currentTab == EManagerTab.Abilities)
            {
                DrawAbilitiesList();
            }
            else
            {
                DrawEffectsList();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAbilitiesList()
        {
            // Toolbar row 2: Config Filters & Tools
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            _showUnassignedOnly = GUILayout.Toggle(_showUnassignedOnly, "Unassigned Only", EditorStyles.toolbarButton, GUILayout.Width(110));
            if (EditorGUI.EndChangeCheck()) ApplySearchFilter();

            if (_config != null)
            {
                if (GUILayout.Button("Add All Missing", EditorStyles.toolbarButton))
                {
                    AddAllMissingToConfig();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Config Status Warning
            if (_config == null)
            {
                EditorGUILayout.HelpBox("AbilitiesConfig not found! Cannot manage assignments.", MessageType.Warning);
            }

            // Count Stats
            EditorGUILayout.LabelField($"Showing {_filteredAbilities.Count} / {_allAbilities.Count} Abilities", EditorStyles.miniLabel);

            // Scrollable List
            _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos, false, true);

            foreach (var ability in _filteredAbilities)
            {
                if (ability == null) continue;

                bool isSelected = (_selectedAsset == ability);
                GUIStyle rowStyle = isSelected ? _selectedRowStyle : _unselectedRowStyle;

                EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(ROW_HEIGHT));
                
                if (GUILayout.Button(ability.name, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(ROW_HEIGHT)))
                {
                    SelectAsset(ability);
                }

                // Add / Remove from Config
                if (_config != null)
                {
                    bool isAssigned = _config.allAbilities.Contains(ability);
                    if (isAssigned)
                    {
                        var oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Light Red
                        if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(ROW_HEIGHT - 4)))
                        {
                            RemoveFromConfig(ability);
                        }
                        GUI.backgroundColor = oldColor;
                    }
                    else
                    {
                        var oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = Color.green;
                        if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(ROW_HEIGHT - 4)))
                        {
                            AddToConfig(ability);
                        }
                        GUI.backgroundColor = oldColor;
                    }
                }

                // Ping Button
                if (GUILayout.Button("P", GUILayout.Width(20), GUILayout.Height(ROW_HEIGHT - 4))) EditorGUIUtility.PingObject(ability);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEffectsList()
        {
            // Effects have no config, just show count
            EditorGUILayout.LabelField($"Showing {_filteredEffects.Count} / {_allEffects.Count} Effects", EditorStyles.miniLabel);

            // Scrollable List
            _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos, false, true);

            foreach (var effect in _filteredEffects)
            {
                if (effect == null) continue;

                bool isSelected = (_selectedAsset == effect);
                GUIStyle rowStyle = isSelected ? _selectedRowStyle : _unselectedRowStyle;

                EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(ROW_HEIGHT));
                
                if (GUILayout.Button(effect.name, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(ROW_HEIGHT)))
                {
                    SelectAsset(effect);
                }

                // Ping Button
                if (GUILayout.Button("P", GUILayout.Width(20), GUILayout.Height(ROW_HEIGHT - 4))) EditorGUIUtility.PingObject(effect);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSeparatorLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            if (_selectedAsset == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select an asset from the list to edit its properties.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            else
            {
                // Scrollable Inspector
                _rightScrollPos = EditorGUILayout.BeginScrollView(_rightScrollPos);

                GUILayout.Space(10);
                
                // Title and Asset Box
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Editing: {_selectedAsset.name}", EditorStyles.boldLabel);
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Asset Reference", _selectedAsset, typeof(UnityEngine.Object), false);
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                // Smart Polymorphic Inspector Frame
                if (_cachedEditor != null)
                {
                    EditorGUI.BeginChangeCheck();
                    
                    _cachedEditor.OnInspectorGUI(); // Polmorphic magic!
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(_selectedAsset);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Failed to create editor for this asset.", MessageType.Error);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        #region Config Management Helpers

        private void AddToConfig(GameplayAbilityData ability)
        {
            if (_config == null || _config.allAbilities.Contains(ability)) return;
            _config.allAbilities.Add(ability);
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            ApplySearchFilter(); 
        }

        private void RemoveFromConfig(GameplayAbilityData ability)
        {
            if (_config == null || !_config.allAbilities.Contains(ability)) return;
            _config.allAbilities.Remove(ability);
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            ApplySearchFilter(); 
        }

        private void AddAllMissingToConfig()
        {
            if (_config == null) return;

            bool addedAny = false;
            foreach (var ability in _allAbilities)
            {
                if (!_config.allAbilities.Contains(ability))
                {
                    _config.allAbilities.Add(ability);
                    addedAny = true;
                }
            }

            if (addedAny)
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                ApplySearchFilter();
                Debug.Log($"[Abilities Manager] Bulk added all missing abilities to {_config.name}.");
            }
        }

        #endregion

        #region UI Helpers

        private void SelectAsset(UnityEngine.Object newSelection)
        {
            if (_selectedAsset == newSelection) return;

            // Cleanup old editor
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }

            _selectedAsset = newSelection;

            if (_selectedAsset != null)
            {
                UnityEditor.Editor.CreateCachedEditor(_selectedAsset, null, ref _cachedEditor);
            }
            
            _rightScrollPos = Vector2.zero;
            GUI.FocusControl(null); 
        }

        private void InitStyles()
        {
            if (_selectedRowStyle == null)
            {
                _selectedRowStyle = new GUIStyle(GUI.skin.box);
                _selectedRowStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.6f, 1f));
                _selectedRowStyle.margin = new RectOffset(2, 2, 0, 0);
            }

            if (_unselectedRowStyle == null)
            {
                _unselectedRowStyle = new GUIStyle(GUIStyle.none);
                _unselectedRowStyle.margin = new RectOffset(2, 2, 0, 0);
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        #endregion
    }
}
#endif
