using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;
using Abel.TowerDefense.Logic;

namespace Abel.TowerDefense.EditorTools
{
    public class GameDataManagerWindow : EditorWindow
    {
        private GameDatabase database;

        // Tab system
        private string[] tabs = { "Units", "Bullets", "Weapons (WIP)", "Effects (WIP)" };
        private int currentTab = 0;

        // UI State
        private UnitProfileData selectedUnit;
        private BulletProfileData selectedBullet;
        private Vector2 scrollPosList;
        private Vector2 scrollPosSettings;
        private string searchText = "";

        // Reflection Caches
        private Type[] unitLogicTypes;
        private string[] unitLogicNames;
        private Type[] bulletLogicTypes;
        private string[] bulletLogicNames;

        [MenuItem("Abel/Game Data Manager")]
        public static void ShowWindow()
        {
            GetWindow<GameDataManagerWindow>("Game Database");
        }

        private void OnEnable()
        {
            LoadDatabase();
            ScanLogicTypes();
        }

        private void LoadDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameDatabase");
            if (guids.Length > 0)
            {
                database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        private void CreateDatabase()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Database", "GameDatabase", "asset", "Create");
            if (!string.IsNullOrEmpty(path))
            {
                database = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(database, path);
                AssetDatabase.SaveAssets();
            }
        }

        private void ScanLogicTypes()
        {
            // Scan Unit Logics
            unitLogicTypes = TypeCache.GetTypesDerivedFrom<UnitGroupBase>().Where(t => !t.IsAbstract).ToArray();
            unitLogicNames = unitLogicTypes.Select(t => t.Name).ToArray();

            // Scan Bullet Logics
            bulletLogicTypes = TypeCache
                .GetTypesDerivedFrom<BulletGroup>()
                .Concat(new[] { typeof(BulletGroup) })
                .Where(t => !t.IsAbstract)
                .ToArray(); 
            bulletLogicNames = bulletLogicTypes.Select(t => t.Name).ToArray();
        }

        private void OnGUI()
        {
            DrawTopToolbar();

            if (database == null)
            {
                EditorGUILayout.HelpBox("No GameDatabase found.", MessageType.Warning);
                if (GUILayout.Button("Create New Database")) CreateDatabase();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            if (GUI.changed) EditorUtility.SetDirty(database);
        }

        private void DrawTopToolbar()
        {
            GUILayout.Space(5);
            currentTab = GUILayout.Toolbar(currentTab, tabs, GUILayout.Height(30));
            GUILayout.Space(5);
        }

        // --- LEFT PANEL (LIST) ---
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(280));

            EditorGUILayout.BeginHorizontal();
            searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(30))) CreateNewItem();
            EditorGUILayout.EndHorizontal();

            scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList);

            if (currentTab == 0 && database.units != null) // UNITS TAB
            {
                foreach (var unit in database.units.ToList())
                {
                    if (!string.IsNullOrEmpty(searchText) && !unit.unitID.ToLower().Contains(searchText.ToLower())) continue;

                    GUI.backgroundColor = (selectedUnit == unit) ? Color.cyan : Color.white;
                    if (GUILayout.Button(unit.unitID, EditorStyles.miniButton, GUILayout.Height(25)))
                    {
                        selectedUnit = unit;
                        GUI.FocusControl(null);
                    }
                }
            }
            else if (currentTab == 1 && database.bullets != null) // BULLETS TAB
            {
                foreach (var bullet in database.bullets.ToList())
                {
                    if (!string.IsNullOrEmpty(searchText) && !bullet.bulletID.ToLower().Contains(searchText.ToLower())) continue;

                    GUI.backgroundColor = (selectedBullet == bullet) ? Color.yellow : Color.white;
                    if (GUILayout.Button(bullet.bulletID, EditorStyles.miniButton, GUILayout.Height(25)))
                    {
                        selectedBullet = bullet;
                        GUI.FocusControl(null);
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // --- RIGHT PANEL (SETTINGS) ---
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            scrollPosSettings = EditorGUILayout.BeginScrollView(scrollPosSettings);

            if (currentTab == 0 && selectedUnit != null)
            {
                DrawUnitSettings();
            }
            else if (currentTab == 1 && selectedBullet != null)
            {
                DrawBulletSettings();
            }
            else
            {
                GUILayout.Label("Select an item to edit.", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawUnitSettings()
        {
            Undo.RecordObject(database, "Modify Unit");
            GUILayout.Label($"Editing Unit: {selectedUnit.unitID}", EditorStyles.boldLabel);

            selectedUnit.unitID = EditorGUILayout.TextField("Unit ID", selectedUnit.unitID);
            selectedUnit.maxCapacity = EditorGUILayout.IntField("Max Capacity", selectedUnit.maxCapacity);

            DrawLogicSelector(ref selectedUnit.logicTypeAQN, ref selectedUnit.logicDisplayName, unitLogicTypes, unitLogicNames);

            EditorGUILayout.Space();
            GUILayout.Label("Visuals", EditorStyles.boldLabel);
            selectedUnit.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", selectedUnit.mesh, typeof(Mesh), false);
            selectedUnit.baseMaterial = (Material)EditorGUILayout.ObjectField("Material", selectedUnit.baseMaterial, typeof(Material), false);
            selectedUnit.animData = (UnitAnimData)EditorGUILayout.ObjectField("Anim Data", selectedUnit.animData, typeof(UnitAnimData), false);

            EditorGUILayout.Space();
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            selectedUnit.baseMoveSpeed = EditorGUILayout.FloatField("Move Speed", selectedUnit.baseMoveSpeed);
            selectedUnit.baseAttackSpeed = EditorGUILayout.FloatField("Attack Speed", selectedUnit.baseAttackSpeed);

            DrawDeleteButton(() => { database.units.Remove(selectedUnit); selectedUnit = null; });
        }

        private void DrawBulletSettings()
        {
            Undo.RecordObject(database, "Modify Bullet");
            GUILayout.Label($"Editing Bullet: {selectedBullet.bulletID}", EditorStyles.boldLabel);

            selectedBullet.bulletID = EditorGUILayout.TextField("Bullet ID", selectedBullet.bulletID);
            selectedBullet.maxCapacity = EditorGUILayout.IntField("Max Capacity", selectedBullet.maxCapacity);

            DrawLogicSelector(ref selectedBullet.logicTypeAQN, ref selectedBullet.logicDisplayName, bulletLogicTypes, bulletLogicNames);

            EditorGUILayout.Space();
            GUILayout.Label("Visuals", EditorStyles.boldLabel);
            selectedBullet.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", selectedBullet.mesh, typeof(Mesh), false);
            selectedBullet.baseMaterial = (Material)EditorGUILayout.ObjectField("Material", selectedBullet.baseMaterial, typeof(Material), false);
            selectedBullet.animData = (UnitAnimData)EditorGUILayout.ObjectField("Anim Data", selectedBullet.animData, typeof(UnitAnimData), false);

            EditorGUILayout.Space();
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            selectedBullet.moveSpeed = EditorGUILayout.FloatField("Move Speed", selectedBullet.moveSpeed);

            DrawDeleteButton(() => { database.bullets.Remove(selectedBullet); selectedBullet = null; });
        }

        // --- HELPER METHODS ---
        private void DrawLogicSelector(ref string aqn, ref string displayName, Type[] types, string[] names)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            GUILayout.Label("Logic Behavior", EditorStyles.miniBoldLabel);
            string localAqn = aqn;
            int currentIndex = Array.FindIndex(types, t => t.AssemblyQualifiedName == localAqn);
            int newIndex = EditorGUILayout.Popup("Logic Class", currentIndex, names);
            if (newIndex >= 0 && newIndex < types.Length)
            {
                aqn = types[newIndex].AssemblyQualifiedName;
                displayName = types[newIndex].Name;
            }
            EditorGUILayout.EndVertical();
        }

        private void CreateNewItem()
        {
            Undo.RecordObject(database, "Add New Item");
            if (currentTab == 0)
            {
                var nu = new UnitProfileData { unitID = "New_Unit_" + database.units.Count };
                database.units.Add(nu);
                selectedUnit = nu;
            }
            else if (currentTab == 1)
            {
                var nb = new BulletProfileData { bulletID = "New_Bullet_" + database.bullets.Count };
                database.bullets.Add(nb);
                selectedBullet = nb;
            }
        }

        private void DrawDeleteButton(Action onDelete)
        {
            EditorGUILayout.Space(20);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("Delete Item") && EditorUtility.DisplayDialog("Delete", "Are you sure?", "Yes", "No"))
            {
                onDelete.Invoke();
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}