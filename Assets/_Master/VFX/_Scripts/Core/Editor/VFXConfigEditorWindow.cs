using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using FD.Modules.VFX;

namespace FD.Modules.VFX.EditorTools
{
    public class VFXConfigEditorWindow : EditorWindow
    {
        private VFXConfigSO _config;
        private SerializedObject _soConfig;

        private Vector2 _scrollListPos;
        private Vector2 _scrollDetailPos;
        private string _searchQuery = "";
        private int _selectedIndex = -1;

        [MenuItem("Tools/Abel/VFX Config Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<VFXConfigEditorWindow>("VFX Editor");
            window.minSize = new Vector2(750, 450);
            window.Show();
        }

        // Auto open if user double clicks the VFXConfigSO in project window
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is VFXConfigSO)
            {
                ShowWindow();
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            AutoFindAsset();
        }

        private void AutoFindAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:VFXConfigSO");
            if (guids.Length > 0)
            {
                _config = AssetDatabase.LoadAssetAtPath<VFXConfigSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (_config != null)
                {
                    _soConfig = new SerializedObject(_config);
                }
            }
        }

        private void OnGUI()
        {
            if (_config == null || _soConfig == null)
            {
                GUILayout.Label("VFXConfigSO asset not found in project.", EditorStyles.boldLabel);
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Find or Create", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    AutoFindAsset();
                    if (_config == null)
                    {
                        // Provide option to create
                        _config = ScriptableObject.CreateInstance<VFXConfigSO>();

                        // Ensure directory exists
                        if (!AssetDatabase.IsValidFolder("Assets/_Master/VFX/Resources/Configs"))
                        {
                            System.IO.Directory.CreateDirectory("Assets/_Master/VFX/Resources/Configs");
                        }

                        AssetDatabase.CreateAsset(_config, "Assets/_Master/VFX/Resources/Configs/VFXConfig.asset");
                        AssetDatabase.SaveAssets();
                        _soConfig = new SerializedObject(_config);
                    }
                }
                return;
            }

            _soConfig.Update();

            EditorGUILayout.BeginHorizontal();

            // Left Pane: Searchable List
            DrawLeftPane();

            // Right Pane: Item Config Detail
            DrawRightPane();

            EditorGUILayout.EndHorizontal();

            _soConfig.ApplyModifiedProperties();
        }

        private void DrawLeftPane()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(280));

            // ----------- Top Bar -----------
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                AutoFindAsset();
            }
            EditorGUILayout.EndHorizontal();

            _scrollListPos = EditorGUILayout.BeginScrollView(_scrollListPos);

            SerializedProperty listProp = _soConfig.FindProperty("VFXList");
            if (listProp == null)
            {
                GUILayout.Label("Invalid List Property");
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            // Cache items and find duplicates
            HashSet<string> seenIds = new HashSet<string>();
            HashSet<string> duplicates = new HashSet<string>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var idProp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("VfxID");
                if (seenIds.Contains(idProp.stringValue))
                    duplicates.Add(idProp.stringValue);
                else
                    seenIds.Add(idProp.stringValue);
            }

            // ----------- Draw List -----------
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty elem = listProp.GetArrayElementAtIndex(i);
                string vfxId = elem.FindPropertyRelative("VfxID").stringValue;
                bool hasAsset = elem.FindPropertyRelative("EffectAsset").objectReferenceValue != null;

                // Filter
                if (!string.IsNullOrEmpty(_searchQuery) &&
                    !vfxId.ToLower().Contains(_searchQuery.ToLower()))
                {
                    continue;
                }

                // Styling
                GUIStyle rowStyle = new GUIStyle(EditorStyles.toolbarButton);
                rowStyle.alignment = TextAnchor.MiddleLeft;
                rowStyle.fontSize = 12;
                rowStyle.padding = new RectOffset(10, 5, 4, 4);

                Color originalColor = GUI.backgroundColor;
                if (_selectedIndex == i)
                {
                    // Tint selection blue-ish
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f);
                    rowStyle.fontStyle = FontStyle.Bold;
                    rowStyle.normal.textColor = Color.white;
                }

                // Determine error prefix
                string errorPrefix = "";
                if (string.IsNullOrEmpty(vfxId)) errorPrefix = "⚠️ [Empty ID] ";
                else if (duplicates.Contains(vfxId)) errorPrefix = "⚠️ [Dup] ";
                else if (!hasAsset) errorPrefix = "❌ [No Asset] ";

                string displayName = errorPrefix + (string.IsNullOrEmpty(vfxId) ? $"Item {i}" : vfxId);

                if (GUILayout.Button(displayName, rowStyle, GUILayout.Height(28)))
                {
                    _selectedIndex = i;
                    GUI.FocusControl(null); // Remove focus to update inspector views correctly
                }

                GUI.backgroundColor = originalColor;
            }

            EditorGUILayout.EndScrollView();

            // ----------- Add Button -----------
            if (GUILayout.Button("＋ Add New VFX", GUILayout.Height(35)))
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                _selectedIndex = listProp.arraySize - 1;

                SerializedProperty newElem = listProp.GetArrayElementAtIndex(_selectedIndex);
                newElem.FindPropertyRelative("VfxID").stringValue = "vfx_new_" + _selectedIndex;
                newElem.FindPropertyRelative("EffectAsset").objectReferenceValue = null;
                newElem.FindPropertyRelative("Scale").floatValue = 1f;
                newElem.FindPropertyRelative("Speed").floatValue = 1f;
                newElem.FindPropertyRelative("IsLoop").boolValue = false;
                newElem.FindPropertyRelative("Preload").boolValue = true;
                newElem.FindPropertyRelative("Duration").floatValue = -1f;

                _scrollListPos.y = float.MaxValue; // Auto scroll to bottom
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRightPane()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            SerializedProperty listProp = _soConfig.FindProperty("VFXList");
            if (listProp == null || _selectedIndex < 0 || _selectedIndex >= listProp.arraySize)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a VFX item from the left to edit", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 16 });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            SerializedProperty elem = listProp.GetArrayElementAtIndex(_selectedIndex);
            string vfxId = elem.FindPropertyRelative("VfxID").stringValue;

            _scrollDetailPos = EditorGUILayout.BeginScrollView(_scrollDetailPos);

            EditorGUILayout.Space(10);

            // Nice Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Editing: {vfxId}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter });
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // Settings
            EditorGUI.BeginChangeCheck();

            SerializedProperty iter = elem.Copy();
            SerializedProperty end = iter.GetEndProperty();

            iter.NextVisible(true);
            while (!SerializedProperty.EqualContents(iter, end))
            {
                // Better layout for properties
                EditorGUILayout.PropertyField(iter, true);
                if (!iter.NextVisible(false))
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                _soConfig.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(40);

            // Delete Button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete Component", GUILayout.Width(180), GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Delete Confirmation", $"Are you sure you want to delete '{vfxId}'?", "Yes", "No"))
                {
                    listProp.DeleteArrayElementAtIndex(_selectedIndex);
                    _soConfig.ApplyModifiedProperties();
                    _selectedIndex = -1;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }
    }
}
