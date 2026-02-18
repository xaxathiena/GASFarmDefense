using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;

namespace Abel.TowerDefense.EditorTools
{
    public class UnitsProfileManagerWindow : EditorWindow
    {
        // Reference tới Database tổng
        private UnitsProfile database;
        
        // UI State
        private UnitProfileData selectedUnit;
        private Vector2 scrollPosList;
        private Vector2 scrollPosSettings;
        private string searchText = "";

        // Reflection Cache
        private Type[] availableLogicTypes;
        private string[] logicTypeNames;

        [MenuItem("Abel/Units Manager (Database)")]
        public static void ShowWindow()
        {
            GetWindow<UnitsProfileManagerWindow>("Units Manager");
        }

        private void OnEnable()
        {
            LoadDatabase();
            ScanLogicTypes();
        }

        private void LoadDatabase()
        {
            // Tự động tìm file UnitsProfile trong project
            string[] guids = AssetDatabase.FindAssets("t:UnitsProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<UnitsProfile>(path);
            }
        }

        private void CreateDatabase()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Units Database", "UnitsDatabase", "asset", "Create");
            if (!string.IsNullOrEmpty(path))
            {
                var newDb = ScriptableObject.CreateInstance<UnitsProfile>();
                AssetDatabase.CreateAsset(newDb, path);
                AssetDatabase.SaveAssets();
                database = newDb;
            }
        }

        private void ScanLogicTypes()
        {
            var types = TypeCache.GetTypesDerivedFrom<UnitGroupBase>();
            availableLogicTypes = types.Where(t => !t.IsAbstract).ToArray();
            logicTypeNames = availableLogicTypes.Select(t => t.Name).ToArray();
        }

        private void OnGUI()
        {
            if (database == null)
            {
                DrawNoDatabase();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            // Xử lý Undo/Dirty để lưu dữ liệu
            if (GUI.changed)
            {
                EditorUtility.SetDirty(database);
            }
        }

        private void DrawNoDatabase()
        {
            EditorGUILayout.HelpBox("No 'UnitsProfile' database found via Search.", MessageType.Warning);
            if (GUILayout.Button("Create New Database")) CreateDatabase();
            if (GUILayout.Button("Refresh Search")) LoadDatabase();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(280));

            // Header
            GUILayout.Label("Units List", EditorStyles.boldLabel);
            
            // Search & Tools
            EditorGUILayout.BeginHorizontal();
            searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(30))) CreateNewUnit();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // List View
            scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList);
            
            if (database.units != null)
            {
                for (int i = 0; i < database.units.Count; i++)
                {
                    var unit = database.units[i];
                    
                    // Filter logic
                    if (!string.IsNullOrEmpty(searchText) && 
                        !unit.unitID.ToLower().Contains(searchText.ToLower())) continue;

                    // Draw Item
                    GUI.backgroundColor = (selectedUnit == unit) ? Color.cyan : Color.white;
                    if (GUILayout.Button(string.IsNullOrEmpty(unit.unitID) ? "Unnamed" : unit.unitID, EditorStyles.miniButton, GUILayout.Height(25)))
                    {
                        selectedUnit = unit;
                        GUI.FocusControl(null);
                    }
                }
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));

            if (selectedUnit == null)
            {
                GUILayout.Label("Select a unit to edit.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                scrollPosSettings = EditorGUILayout.BeginScrollView(scrollPosSettings);

                // Title
                GUILayout.Label($"Editing: {selectedUnit.unitID}", EditorStyles.boldLabel);
                EditorGUILayout.Space(10);

                // --- RECORD UNDO ---
                Undo.RecordObject(database, "Modify Unit Profile");

                // --- 1. IDENTITY ---
                selectedUnit.unitID = EditorGUILayout.TextField("Unit ID", selectedUnit.unitID);

                // --- 2. LOGIC REFLECTION ---
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical("HelpBox");
                GUILayout.Label("Logic Behavior", EditorStyles.miniBoldLabel);

                int currentIndex = -1;
                for (int i = 0; i < availableLogicTypes.Length; i++)
                {
                    if (availableLogicTypes[i].AssemblyQualifiedName == selectedUnit.logicTypeAQN)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                int newIndex = EditorGUILayout.Popup("Logic Class", currentIndex, logicTypeNames);
                if (newIndex >= 0 && newIndex < availableLogicTypes.Length)
                {
                    var type = availableLogicTypes[newIndex];
                    selectedUnit.logicTypeAQN = type.AssemblyQualifiedName;
                    selectedUnit.logicDisplayName = type.Name;
                }
                EditorGUILayout.EndVertical();

                // --- 3. VISUALS & STATS (Manual Drawing) ---
                // Vì class này không phải là UnityEngine.Object nên không dùng SerializedObject đơn giản được
                // Ta vẽ thủ công các field
                
                EditorGUILayout.Space(5);
                GUILayout.Label("Visuals", EditorStyles.boldLabel);
                selectedUnit.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", selectedUnit.mesh, typeof(Mesh), false);
                selectedUnit.baseMaterial = (Material)EditorGUILayout.ObjectField("Base Material", selectedUnit.baseMaterial, typeof(Material), false);
                selectedUnit.animData = (UnitAnimData)EditorGUILayout.ObjectField("Anim Data", selectedUnit.animData, typeof(UnitAnimData), false);

                EditorGUILayout.Space(5);
                GUILayout.Label("Stats", EditorStyles.boldLabel);
                selectedUnit.baseMoveSpeed = EditorGUILayout.FloatField("Move Speed", selectedUnit.baseMoveSpeed);
                selectedUnit.baseAttackSpeed = EditorGUILayout.FloatField("Attack Speed", selectedUnit.baseAttackSpeed);

                // --- DELETE BUTTON ---
                EditorGUILayout.Space(20);
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Delete Unit"))
                {
                    if (EditorUtility.DisplayDialog("Delete", $"Delete {selectedUnit.unitID}?", "Yes", "No"))
                    {
                        database.units.Remove(selectedUnit);
                        selectedUnit = null;
                        GUIUtility.ExitGUI(); // Thoát ngay để tránh lỗi vẽ tiếp
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewUnit()
        {
            Undo.RecordObject(database, "Add New Unit");
            var newUnit = new UnitProfileData();
            newUnit.unitID = "New_Unit_" + database.units.Count;
            database.units.Add(newUnit);
            selectedUnit = newUnit;
        }
    }
}