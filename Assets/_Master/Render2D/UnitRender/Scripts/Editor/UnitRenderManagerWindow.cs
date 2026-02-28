using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;
using Abel.TowerDefense.Data; // Ensure this is included for UnitAnimData

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

        // --- NEW: Caching for Preview & Embedded Editor ---
        private Editor animDataEditor;
        private Texture2D cachedPreviewTexture;
        private UnitRenderProfileData lastPreviewedProfile;

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

        private void OnDisable()
        {
            // Clean up memory to prevent Editor leaks
            if (cachedPreviewTexture != null) DestroyImmediate(cachedPreviewTexture);
            if (animDataEditor != null) DestroyImmediate(animDataEditor);
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

            List<string> filterOptions = new List<string> { "All", "Common" };
            filterOptions.AddRange(database.definedPrefixes);

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
                    bool isCommon = !database.definedPrefixes.Any(p => profile.unitID.StartsWith(p));

                    if (currentFilterIndex == 1 && !isCommon) continue; 
                    if (currentFilterIndex > 1) 
                    {
                        string activePrefix = filterOptions[currentFilterIndex];
                        if (!profile.unitID.StartsWith(activePrefix)) continue;
                    }

                    if (!string.IsNullOrEmpty(searchText) && !profile.unitID.ToLower().Contains(searchText.ToLower())) continue;

                    GUI.backgroundColor = GetColorForID(profile.unitID, profile == selectedProfile);

                    if (GUILayout.Button(string.IsNullOrEmpty(profile.unitID) ? "Unnamed" : profile.unitID, EditorStyles.miniButton, GUILayout.Height(25)))
                    {
                        if (selectedProfile != profile) lastPreviewedProfile = null; // Force refresh preview
                        selectedProfile = profile;
                        isAddingNewPrefix = false; 
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

            // --- HEADER ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Editing: {selectedProfile.unitID}", EditorStyles.boldLabel);
            if (GUILayout.Button("Copy ID", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                EditorGUIUtility.systemCopyBuffer = selectedProfile.unitID;
                Debug.Log($"[Unit Render Manager] Copied '{selectedProfile.unitID}' to clipboard!");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);

            // --- TOP SECTION (2 COLUMNS: PREVIEW | BASIC SETTINGS) ---
            EditorGUILayout.BeginHorizontal();
            
            // COLUMN 1: PREVIEW BOX
            EditorGUILayout.BeginVertical("box", GUILayout.Width(130));
            DrawPreviewBox();
            EditorGUILayout.EndVertical();

            // COLUMN 2: IDENTITY & VISUAL RESOURCES
            EditorGUILayout.BeginVertical();
            DrawIdentitySection();
            
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginVertical("HelpBox");
            GUILayout.Label("Core Settings", EditorStyles.miniBoldLabel);
            selectedProfile.maxCapacity = EditorGUILayout.IntField("Max Capacity", selectedProfile.maxCapacity);
            
            EditorGUI.BeginChangeCheck();
            selectedProfile.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", selectedProfile.mesh, typeof(Mesh), false);
            selectedProfile.baseMaterial = (Material)EditorGUILayout.ObjectField("Material", selectedProfile.baseMaterial, typeof(Material), false);
            selectedProfile.animData = (UnitAnimData)EditorGUILayout.ObjectField("Anim Data", selectedProfile.animData, typeof(UnitAnimData), false);
            selectedProfile.showHealthBar = EditorGUILayout.Toggle("Show Health Bar", selectedProfile.showHealthBar);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Refresh preview if they drag-dropped a new AnimData
                lastPreviewedProfile = null; 
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical(); // End Column 2
            EditorGUILayout.EndHorizontal(); // End Top Section

            EditorGUILayout.Space(10);

            // --- EMBEDDED ANIM DATA EDITOR ---
            DrawEmbeddedAnimDataEditor();

            EditorGUILayout.Space(10);

            // --- STATS SECTION ---
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("HelpBox");
            selectedProfile.baseMoveSpeed = EditorGUILayout.FloatField("Move Speed", selectedProfile.baseMoveSpeed);
            selectedProfile.baseAttackSpeed = EditorGUILayout.FloatField("Attack Speed", selectedProfile.baseAttackSpeed);
            EditorGUILayout.EndVertical();

            DrawDeleteButton(() => { database.units.Remove(selectedProfile); selectedProfile = null; });
        }

        // --- NEW: PREVIEW GENERATOR ---
        private void DrawPreviewBox()
        {
            GUILayout.Label("Preview", EditorStyles.miniBoldLabel);

            // Try to generate texture from the Texture2DArray inside AnimData
            if (lastPreviewedProfile != selectedProfile)
            {
                if (cachedPreviewTexture != null) DestroyImmediate(cachedPreviewTexture);
                cachedPreviewTexture = null;
                lastPreviewedProfile = selectedProfile;

                if (selectedProfile.animData != null && selectedProfile.animData.textureArray != null)
                {
                    try
                    {
                        Texture2DArray texArray = selectedProfile.animData.textureArray;
                        // Create a temporary Texture2D with matching format to copy the first slice (frame 0)
                        cachedPreviewTexture = new Texture2D(texArray.width, texArray.height, texArray.format, false);
                        Graphics.CopyTexture(texArray, 0, 0, cachedPreviewTexture, 0, 0);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Unit Render Manager] Could not extract preview: {ex.Message}");
                    }
                }
            }

            // Draw the texture if available
            if (cachedPreviewTexture != null)
            {
                Rect rect = GUILayoutUtility.GetRect(115, 115);
                GUI.DrawTexture(rect, cachedPreviewTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Label("No Texture\nAssigned", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(115));
            }
        }

        // --- NEW: EMBEDDED ANIM DATA EDITOR ---
        private void DrawEmbeddedAnimDataEditor()
        {
            if (selectedProfile.animData == null) return;

            GUILayout.Label("Animation Settings (Embedded)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUI.BeginChangeCheck();
            
            // Create or update the cached editor for the ScriptableObject
            if (animDataEditor == null || animDataEditor.target != selectedProfile.animData)
            {
                Editor.CreateCachedEditor(selectedProfile.animData, null, ref animDataEditor);
            }

            // Draw the inspector natively
            animDataEditor.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                // Save the SO if user tweaked values inside our tool
                EditorUtility.SetDirty(selectedProfile.animData);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            GUILayout.Label("Identity", EditorStyles.miniBoldLabel);

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

            List<string> prefixOptions = new List<string> { "[Common]" };
            prefixOptions.AddRange(database.definedPrefixes);
            prefixOptions.Add("[+ Add New Prefix]");

            int selectedPrefixIdx = currentPrefix == "" ? 0 : database.definedPrefixes.IndexOf(currentPrefix) + 1;

            EditorGUI.BeginChangeCheck(); 
            int newSelectedIdx = EditorGUILayout.Popup("Prefix", selectedPrefixIdx, prefixOptions.ToArray());

            if (EditorGUI.EndChangeCheck())
            {
                if (newSelectedIdx == prefixOptions.Count - 1)
                {
                    isAddingNewPrefix = true; 
                }
                else
                {
                    isAddingNewPrefix = false; 
                    currentPrefix = newSelectedIdx == 0 ? "" : database.definedPrefixes[newSelectedIdx - 1];
                    selectedProfile.unitID = currentPrefix + baseName;
                    GUI.FocusControl(null); 
                }
            }

            if (isAddingNewPrefix)
            {
                EditorGUILayout.BeginHorizontal();
                newPrefixInput = EditorGUILayout.TextField(" ", newPrefixInput);

                if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(newPrefixInput) && !database.definedPrefixes.Contains(newPrefixInput))
                    {
                        database.definedPrefixes.Add(newPrefixInput);
                        currentPrefix = newPrefixInput;
                        selectedProfile.unitID = currentPrefix + baseName;
                        isAddingNewPrefix = false; 
                        newPrefixInput = "";
                        GUI.FocusControl(null);
                    }
                }

                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    isAddingNewPrefix = false;
                    newPrefixInput = "";
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            baseName = EditorGUILayout.TextField("Base Name", baseName);
            if (EditorGUI.EndChangeCheck())
            {
                selectedProfile.unitID = currentPrefix + baseName;
            }

            if (GUILayout.Button("Auto Fill", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                baseName = baseName.ToLower();
                selectedProfile.unitID = currentPrefix + baseName;
                AutoFillVisualAssets(selectedProfile, baseName);
                lastPreviewedProfile = null; // Refresh preview after autofill
                GUI.FocusControl(null); 
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateNewItem()
        {
            Undo.RecordObject(database, "Add New Item");

            string prefix = "";
            if (currentFilterIndex > 1)
            {
                prefix = database.definedPrefixes[currentFilterIndex - 2];
            }

            var newProfile = new UnitRenderProfileData { unitID = prefix + "new_" + database.units.Count };
            database.units.Add(newProfile);
            selectedProfile = newProfile;
            lastPreviewedProfile = null;
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

        private Color GetColorForID(string id, bool isSelected)
        {
            if (isSelected) return Color.cyan;

            string currentPrefix = "";
            foreach (string p in database.definedPrefixes)
            {
                if (id.StartsWith(p)) { currentPrefix = p; break; }
            }

            if (string.IsNullOrEmpty(currentPrefix)) return Color.white; 

            UnityEngine.Random.InitState(currentPrefix.GetHashCode());
            float h = UnityEngine.Random.Range(0f, 1f);
            return Color.HSVToRGB(h, 0.2f, 1.0f); 
        }

        private void AutoFillVisualAssets(UnitRenderProfileData profile, string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return;

            Undo.RecordObject(database, "Auto Fill Visuals");
            int filledCount = 0;

            if (profile.mesh == null)
            {
                profile.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                if (profile.mesh != null) filledCount++;
            }

            string matSearchString = $"{baseName}_Material t:Material";
            string[] matGuids = AssetDatabase.FindAssets(matSearchString);
            if (matGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                profile.baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                filledCount++;
            }

            string animSearchString = $"{baseName}_Data t:UnitAnimData";
            string[] animGuids = AssetDatabase.FindAssets(animSearchString);
            if (animGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(animGuids[0]);
                profile.animData = AssetDatabase.LoadAssetAtPath<UnitAnimData>(path);
                filledCount++;
            }

            if (filledCount > 0) Debug.Log($"[Auto Fill] Successfully filled {filledCount} assets for {baseName}.");
        }
    }
}