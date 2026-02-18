    using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;

namespace Abel.TowerDefense.EditorTools
{
    public class UnitProfileManagerWindow : EditorWindow
    {
        private List<UnitProfile> profiles = new List<UnitProfile>();
        private UnitProfile selectedProfile;
        private Vector2 scrollPosList;
        private Vector2 scrollPosSettings;

        // Reflection Cache
        private Type[] availableLogicTypes;
        private string[] logicTypeNames;

        [MenuItem("Abel/Unit Profile Manager")]
        public static void ShowWindow()
        {
            GetWindow<UnitProfileManagerWindow>("Unit Profiles");
        }

        private void OnEnable()
        {
            RefreshProfileList();
            ScanLogicTypes();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // --- LEFT PANEL: LIST ---
            DrawLeftPanel();

            // --- RIGHT PANEL: SETTINGS ---
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        // 1. Quét tất cả class kế thừa từ UnitGroupBase
        private void ScanLogicTypes()
        {
            // TypeCache cực nhanh trong Unity Editor mới
            var types = TypeCache.GetTypesDerivedFrom<UnitGroupBase>();
            
            // Lọc bỏ class abstract nếu có
            availableLogicTypes = types.Where(t => !t.IsAbstract).ToArray();
            
            // Tạo danh sách tên để hiển thị lên Popup
            logicTypeNames = availableLogicTypes.Select(t => t.Name).ToArray();
        }

        private void RefreshProfileList()
        {
            profiles.Clear();
            string[] guids = AssetDatabase.FindAssets("t:UnitProfile");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                profiles.Add(AssetDatabase.LoadAssetAtPath<UnitProfile>(path));
            }
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(250));
            
            // Toolbar
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Profiles", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(30))) CreateNewProfile();
            if (GUILayout.Button("↻", GUILayout.Width(30))) RefreshProfileList();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // List
            scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList);
            foreach (var p in profiles)
            {
                if (p == null) continue;
                
                GUI.backgroundColor = (selectedProfile == p) ? Color.cyan : Color.white;
                if (GUILayout.Button(string.IsNullOrEmpty(p.unitID) ? p.name : p.unitID, EditorStyles.miniButton, GUILayout.Height(25)))
                {
                    selectedProfile = p;
                    GUI.FocusControl(null); // Bỏ focus để update data
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));

            if (selectedProfile == null)
            {
                GUILayout.Label("Select a profile to edit.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                scrollPosSettings = EditorGUILayout.BeginScrollView(scrollPosSettings);

                GUILayout.Label($"Editing: {selectedProfile.name}", EditorStyles.boldLabel);
                EditorGUILayout.Space(10);

                EditorGUI.BeginChangeCheck();

                // --- LOGIC CLASS SELECTOR (REFLECTION DROPDOWN) ---
                EditorGUILayout.BeginVertical("HelpBox");
                GUILayout.Label("Logic Configuration", EditorStyles.boldLabel);
                
                int currentIndex = -1;
                // Tìm index hiện tại của logic đang lưu trong profile
                for(int i=0; i<availableLogicTypes.Length; i++)
                {
                    if(availableLogicTypes[i].AssemblyQualifiedName == selectedProfile.logicTypeAQN)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                int newIndex = EditorGUILayout.Popup("Logic Behavior Class", currentIndex, logicTypeNames);
                
                if (newIndex >= 0 && newIndex < availableLogicTypes.Length)
                {
                    var selectedType = availableLogicTypes[newIndex];
                    selectedProfile.logicTypeAQN = selectedType.AssemblyQualifiedName;
                    selectedProfile.logicDisplayName = selectedType.Name;
                }
                else if (currentIndex == -1) 
                {
                    EditorGUILayout.HelpBox("Please select a Logic Class!", MessageType.Error);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // --- DEFAULT INSPECTOR FOR OTHER FIELDS ---
                // Vẽ tất cả các field còn lại (Mesh, Material...) dùng Editor mặc định
                SerializedObject so = new SerializedObject(selectedProfile);
                SerializedProperty prop = so.GetIterator();
                bool enterChildren = true;
                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    // Bỏ qua field script mặc định và các field logic ta đã vẽ custom
                    if (prop.name == "m_Script" || prop.name == "logicTypeAQN" || prop.name == "logicDisplayName") continue;
                    
                    EditorGUILayout.PropertyField(prop, true);
                }
                so.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedProfile);
                }

                // Delete Button
                EditorGUILayout.Space(20);
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Delete Profile"))
                {
                    if (EditorUtility.DisplayDialog("Delete", $"Delete {selectedProfile.name}?", "Yes", "No"))
                    {
                        string path = AssetDatabase.GetAssetPath(selectedProfile);
                        AssetDatabase.DeleteAsset(path);
                        selectedProfile = null;
                        RefreshProfileList();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Unit Profile", "NewUnitProfile", "asset", "Save Profile");
            if (!string.IsNullOrEmpty(path))
            {
                var newProfile = ScriptableObject.CreateInstance<UnitProfile>();
                newProfile.unitID = "New_Unit";
                AssetDatabase.CreateAsset(newProfile, path);
                AssetDatabase.SaveAssets();
                RefreshProfileList();
                selectedProfile = newProfile;
            }
        }
    }
}