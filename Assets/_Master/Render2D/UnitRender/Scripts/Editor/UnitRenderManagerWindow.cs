using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;

namespace Abel.TowerDefense.EditorTools
{
    public class UnitRenderManagerWindow : EditorWindow
    {
        private UnitRenderDatabase database;

        // UI State
        private UnitRenderProfileData selectedProfile;
        private Vector2 scrollPosList;
        private Vector2 scrollPosSettings;
        private string searchText = "";

        // Filter System
        private int currentFilterIndex = 0;

        // Prefix Creation System
        private bool isAddingNewPrefix = false;
        private string newPrefixInput = "";

        // Reflection Caches
        private Type[] logicTypes;
        private string[] logicNames;

        [MenuItem("Tools/Abel/Render/Unit Render Manager")]
        public static void ShowWindow()
        {
            GetWindow<UnitRenderManagerWindow>("Unit Render Manager");
        }

        private void OnEnable()
        {
            LoadDatabase();
            ScanLogicTypes();
        }

        private void LoadDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:UnitRenderDatabase");
            if (guids.Length > 0)
            {
                database = AssetDatabase.LoadAssetAtPath<UnitRenderDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                Debug.Log($"Loaded UnitRenderDatabase: {database.name}");
            }
        }

        private void ScanLogicTypes()
        {
            logicTypes = TypeCache.GetTypesDerivedFrom<UnitGroupBase>().Where(t => !t.IsAbstract).ToArray();
            logicNames = logicTypes.Select(t => t.Name).ToArray();
        }

        private void OnGUI()
        {
            if (database == null)
            {
                EditorGUILayout.HelpBox("No UnitRenderDatabase found.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            if (GUI.changed) EditorUtility.SetDirty(database);
        }

        // --- LEFT PANEL (LIST & FILTERS) ---
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(300));

            // Build dynamic filter options: "All", "Common", and all defined prefixes
            List<string> filterOptions = new List<string> { "All", "Common" };
            filterOptions.AddRange(database.definedPrefixes);

            // Clamp filter index in case a prefix was deleted
            currentFilterIndex = Mathf.Clamp(currentFilterIndex, 0, filterOptions.Count - 1);

            EditorGUILayout.BeginHorizontal();
            currentFilterIndex = EditorGUILayout.Popup(currentFilterIndex, filterOptions.ToArray(), GUILayout.Width(90));
            searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(30))) CreateNewItem();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            scrollPosList = EditorGUILayout.BeginScrollView(scrollPosList);

            if (database.units != null)
            {
                foreach (var profile in database.units.ToList())
                {
                    // 1. Determine if this profile is "Common" (does not start with any defined prefix)
                    bool isCommon = !database.definedPrefixes.Any(p => profile.unitID.StartsWith(p));

                    // 2. Filter logic
                    if (currentFilterIndex == 1 && !isCommon) continue; // Filter: Common
                    if (currentFilterIndex > 1) // Filter: Specific Prefix
                    {
                        string activePrefix = filterOptions[currentFilterIndex];
                        if (!profile.unitID.StartsWith(activePrefix)) continue;
                    }

                    // Search text logic
                    if (!string.IsNullOrEmpty(searchText) && !profile.unitID.ToLower().Contains(searchText.ToLower())) continue;

                    // 3. UI Drawing with dynamic colors
                    GUI.backgroundColor = GetColorForID(profile.unitID, profile == selectedProfile);

                    if (GUILayout.Button(string.IsNullOrEmpty(profile.unitID) ? "Unnamed" : profile.unitID, EditorStyles.miniButton, GUILayout.Height(25)))
                    {
                        selectedProfile = profile;
                        isAddingNewPrefix = false; // Reset state when selecting new item
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

            if (selectedProfile != null)
            {
                DrawProfileSettings();
            }
            else
            {
                GUILayout.Label("Select an item to edit.", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawProfileSettings()
        {
            Undo.RecordObject(database, "Modify Profile");
            
            // --- THÊM CHỨC NĂNG COPY TẠI ĐÂY ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Editing: {selectedProfile.unitID}", EditorStyles.boldLabel);
            
            // Nút Copy
            if (GUILayout.Button("Copy ID", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                EditorGUIUtility.systemCopyBuffer = selectedProfile.unitID;
                Debug.Log($"[Unit Render Manager] Copied '{selectedProfile.unitID}' to clipboard!");
            }
            EditorGUILayout.EndHorizontal();
            // ----------------------------------

            EditorGUILayout.Space(5);

            // --- PREFIX & IDENTITY SYSTEM ---
            DrawIdentitySection();

            selectedProfile.maxCapacity = EditorGUILayout.IntField("Max Capacity", selectedProfile.maxCapacity);

            // DrawLogicSelector(ref selectedProfile.logicTypeAQN, ref selectedProfile.logicDisplayName, logicTypes, logicNames);

            EditorGUILayout.Space();
            GUILayout.Label("Visuals", EditorStyles.boldLabel);
            selectedProfile.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", selectedProfile.mesh, typeof(Mesh), false);
            selectedProfile.baseMaterial = (Material)EditorGUILayout.ObjectField("Material", selectedProfile.baseMaterial, typeof(Material), false);
            selectedProfile.animData = (UnitAnimData)EditorGUILayout.ObjectField("Anim Data", selectedProfile.animData, typeof(UnitAnimData), false);

            EditorGUILayout.Space();
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            selectedProfile.baseMoveSpeed = EditorGUILayout.FloatField("Move Speed", selectedProfile.baseMoveSpeed);
            selectedProfile.baseAttackSpeed = EditorGUILayout.FloatField("Attack Speed", selectedProfile.baseAttackSpeed);

            DrawDeleteButton(() => { database.units.Remove(selectedProfile); selectedProfile = null; });
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            GUILayout.Label("Identity", EditorStyles.miniBoldLabel);

            // 1. Extract current prefix and base name
            string currentPrefix = "";
            string baseName = selectedProfile.unitID;

            foreach (string p in database.definedPrefixes)
            {
                if (selectedProfile.unitID.StartsWith(p))
                {
                    currentPrefix = p;
                    baseName = selectedProfile.unitID.Substring(p.Length);
                    break;
                }
            }

            // 2. Build Prefix Dropdown Options
            List<string> prefixOptions = new List<string> { "[Common]" };
            prefixOptions.AddRange(database.definedPrefixes);
            prefixOptions.Add("[+ Add New Prefix]");

            int selectedPrefixIdx = currentPrefix == "" ? 0 : database.definedPrefixes.IndexOf(currentPrefix) + 1;

            // 3. Draw Dropdown and handle changes properly
            EditorGUI.BeginChangeCheck(); // Start tracking user interaction
            int newSelectedIdx = EditorGUILayout.Popup("Prefix", selectedPrefixIdx, prefixOptions.ToArray());

            // Only execute this block if the user ACTUALLY changed the dropdown selection
            if (EditorGUI.EndChangeCheck())
            {
                if (newSelectedIdx == prefixOptions.Count - 1)
                {
                    isAddingNewPrefix = true; // Show the input field
                }
                else
                {
                    isAddingNewPrefix = false; // Hide the input field
                    currentPrefix = newSelectedIdx == 0 ? "" : database.definedPrefixes[newSelectedIdx - 1];
                    selectedProfile.unitID = currentPrefix + baseName;
                    GUI.FocusControl(null); // Remove focus to update UI
                }
            }

            // 4. Draw Add New Prefix UI
            if (isAddingNewPrefix)
            {
                EditorGUILayout.BeginHorizontal();
                newPrefixInput = EditorGUILayout.TextField(" ", newPrefixInput);

                // Add Button
                if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(newPrefixInput) && !database.definedPrefixes.Contains(newPrefixInput))
                    {
                        database.definedPrefixes.Add(newPrefixInput);
                        currentPrefix = newPrefixInput;
                        selectedProfile.unitID = currentPrefix + baseName;
                        isAddingNewPrefix = false; // Hide after adding
                        newPrefixInput = "";
                        GUI.FocusControl(null);
                    }
                }

                // Cancel Button (UX Improvement)
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    isAddingNewPrefix = false;
                    newPrefixInput = "";
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // 5. Draw Base Name input
            // 5. Draw Base Name input & Auto Fill Button
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            baseName = EditorGUILayout.TextField("Base Name", baseName);
            if (EditorGUI.EndChangeCheck())
            {
                selectedProfile.unitID = currentPrefix + baseName;
            }

            // Auto Fill Button
            if (GUILayout.Button("Auto Fill", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                // Force base name to lower case
                baseName = baseName.ToLower();

                // Update the actual profile ID to reflect the lowercase base name
                selectedProfile.unitID = currentPrefix + baseName;

                // Pass the lowercase base name to the search function
                AutoFillVisualAssets(selectedProfile, baseName);

                GUI.FocusControl(null); // Remove focus to refresh UI immediately
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // --- HELPER METHODS ---

        private void DrawLogicSelector(ref string aqn, ref string displayName, Type[] types, string[] names)
        {
            EditorGUILayout.Space(5);
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

            // Auto-assign prefix based on current filter (if it's not All or Common)
            string prefix = "";
            if (currentFilterIndex > 1)
            {
                prefix = database.definedPrefixes[currentFilterIndex - 2];
            }

            var newProfile = new UnitRenderProfileData { unitID = prefix + "New_" + database.units.Count };
            database.units.Add(newProfile);
            selectedProfile = newProfile;
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

        /// <summary>
        /// Generates a stable pastel color based on the prefix string hash.
        /// </summary>
        private Color GetColorForID(string id, bool isSelected)
        {
            if (isSelected) return Color.cyan;

            // Find prefix to determine color
            string currentPrefix = "";
            foreach (string p in database.definedPrefixes)
            {
                if (id.StartsWith(p))
                {
                    currentPrefix = p;
                    break;
                }
            }

            if (string.IsNullOrEmpty(currentPrefix)) return Color.white; // Common items are white

            // Generate a unique, stable pastel color from the prefix string
            UnityEngine.Random.InitState(currentPrefix.GetHashCode());
            float h = UnityEngine.Random.Range(0f, 1f);
            return Color.HSVToRGB(h, 0.2f, 1.0f); // Low saturation, high value for pastel colors
        }
        /// <summary>
        /// Automatically finds and assigns Mesh, Material, and AnimData based on the baseName.
        /// </summary>
        private void AutoFillVisualAssets(UnitRenderProfileData profile, string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                Debug.LogWarning("[Auto Fill] Base name is empty!");
                return;
            }

            Undo.RecordObject(database, "Auto Fill Visuals");
            int filledCount = 0;

            // 1. Auto assign Quad Mesh (Built-in Unity asset)
            if (profile.mesh == null)
            {
                profile.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                if (profile.mesh != null) filledCount++;
            }

            // 2. Auto assign Material (Searches for "baseName_Material")
            string matSearchString = $"{baseName}_Material t:Material";
            string[] matGuids = AssetDatabase.FindAssets(matSearchString);
            if (matGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                profile.baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                filledCount++;
            }
            else
            {
                Debug.LogWarning($"[Auto Fill] Could not find Material named: {baseName}_Material");
            }

            // 3. Auto assign AnimData (Searches for "baseName_Data")
            string animSearchString = $"{baseName}_Data t:UnitAnimData";
            string[] animGuids = AssetDatabase.FindAssets(animSearchString);
            if (animGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(animGuids[0]);
                profile.animData = AssetDatabase.LoadAssetAtPath<UnitAnimData>(path);
                filledCount++;
            }
            else
            {
                Debug.LogWarning($"[Auto Fill] Could not find UnitAnimData named: {baseName}_Data");
            }

            // Notify user
            if (filledCount > 0)
            {
                Debug.Log($"[Auto Fill] Successfully filled {filledCount} assets for {baseName}.");
            }
        }
    }
}