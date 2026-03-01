using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Abel.TowerDefense.Config;
using Abel.TranHuongDao.Core;
using System.IO;
using System;
using System.Globalization;

namespace Abel.TranHuongDao.EditorTools
{
    public class UnitsConfigEditorWindow : EditorWindow
    {
        private enum ViewMode { TierList, EditUnit }

        private UnitsConfig _config;
        private UnitRenderDatabase _renderDb;
        private SerializedObject _soConfig;

        private ViewMode _currentView = ViewMode.TierList;
        private int _editingIndex = -1;

        private string _searchQuery = "";
        private string _prefixFilter = "";
        private Vector2 _scrollPos;

        // Bộ nhớ cache để lưu texture lấy từ array
        private Dictionary<string, Texture2D> _portraitCache = new Dictionary<string, Texture2D>();

        [MenuItem("Tools/Abel/Units Config Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnitsConfigEditorWindow>("Units Editor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            AutoFindAssets();
        }

        private void AutoFindAssets()
        {
            // Tìm UnitsConfig
            string[] configGuids = AssetDatabase.FindAssets("t:UnitsConfig");
            if (configGuids.Length > 0)
            {
                _config = AssetDatabase.LoadAssetAtPath<UnitsConfig>(AssetDatabase.GUIDToAssetPath(configGuids[0]));
                if (_config != null)
                {
                    _soConfig = new SerializedObject(_config);
                }
            }

            // Tìm UnitRenderDatabase
            string[] dbGuids = AssetDatabase.FindAssets("t:UnitRenderDatabase");
            if (dbGuids.Length > 0)
            {
                _renderDb = AssetDatabase.LoadAssetAtPath<UnitRenderDatabase>(AssetDatabase.GUIDToAssetPath(dbGuids[0]));
            }
        }

        private void OnGUI()
        {
            if (_config == null || _soConfig == null)
            {
                GUILayout.Label("UnitsConfig asset not found. Please create one.", EditorStyles.boldLabel);
                if (GUILayout.Button("Find Manually"))
                {
                    AutoFindAssets();
                }
                return;
            }

            _soConfig.Update();

            EditorGUILayout.Space(5);
            DrawTopBar();

            if (_currentView == ViewMode.TierList)
            {
                DrawTierListView();
            }
            else
            {
                DrawEditView();
            }

            _soConfig.ApplyModifiedProperties();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (_currentView == ViewMode.EditUnit)
            {
                if (GUILayout.Button("◄ Back to List", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    _currentView = ViewMode.TierList;
                    _editingIndex = -1;
                    GUI.FocusControl(null); // Clear focus
                }
            }
            else
            {
                GUILayout.Label("Prefix:", GUILayout.Width(40));
                _prefixFilter = EditorGUILayout.TextField(_prefixFilter, EditorStyles.toolbarTextField, GUILayout.Width(70));
                if (GUILayout.Button("▼", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("All (Clear)"), false, () => { _prefixFilter = ""; });
                    menu.AddItem(new GUIContent("unit_"), false, () => { _prefixFilter = "unit_"; });
                    menu.AddItem(new GUIContent("bullet_"), false, () => { _prefixFilter = "bullet_"; });
                    menu.AddItem(new GUIContent("effect_"), false, () => { _prefixFilter = "effect_"; });
                    menu.AddItem(new GUIContent("weapon_"), false, () => { _prefixFilter = "weapon_"; });
                    menu.ShowAsContext();
                }

                GUILayout.Space(5);

                GUILayout.Label("Search ID:", GUILayout.Width(65));
                _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(150));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Import from CSV", EditorStyles.toolbarButton, GUILayout.Width(120)))
                {
                    ImportCSV();
                }

                if (GUILayout.Button("Force Refresh", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    ClearCache();
                    AutoFindAssets();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTierListView()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(10);

            SerializedProperty entriesProp = _soConfig.FindProperty("unitEntries");

            // Nhóm các đơn vị lại theo Tier
            var tierGroups = new Dictionary<int, List<int>>();

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty elem = entriesProp.GetArrayElementAtIndex(i);
                SerializedProperty tierProp = elem.FindPropertyRelative("Tier");
                SerializedProperty idProp = elem.FindPropertyRelative("UnitID");

                string unitId = idProp.stringValue;

                // Lọc theo prefix
                if (!string.IsNullOrEmpty(_prefixFilter) &&
                    !unitId.StartsWith(_prefixFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Lọc theo search (bỏ qua nếu không thỏa mãn)
                if (!string.IsNullOrEmpty(_searchQuery) &&
                    !unitId.ToLower().Contains(_searchQuery.ToLower()))
                {
                    continue;
                }

                int tier = tierProp.intValue;
                if (!tierGroups.ContainsKey(tier))
                    tierGroups[tier] = new List<int>();

                tierGroups[tier].Add(i);
            }

            var sortedTiers = tierGroups.Keys.ToList();
            sortedTiers.Sort((a, b) => b.CompareTo(a)); // Z->A Để Tier to lên đầu

            foreach (int tier in sortedTiers)
            {
                DrawTierRow(tier, tierGroups[tier], entriesProp);
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add New Unit (Defaults to Tier 1)", GUILayout.Width(250), GUILayout.Height(30)))
            {
                AddNewUnit(1);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTierRow(int tier, List<int> entryIndices, SerializedProperty entriesProp)
        {
            EditorGUILayout.BeginVertical("box");

            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Tier {tier}", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Giao diện tự động xuống dòng khi hết chỗ
            GUILayout.BeginHorizontal();

            float windowWidth = EditorGUIUtility.currentViewWidth;
            float currentWidth = 100; // Account for the label margin

            float iconSize = 80f;
            float margin = 5f;

            foreach (int index in entryIndices)
            {
                SerializedProperty elem = entriesProp.GetArrayElementAtIndex(index);
                string unitID = elem.FindPropertyRelative("UnitID").stringValue;

                // Bẻ xuống dòng mới nếu quá chật
                if (currentWidth + iconSize + margin > windowWidth - 40)
                {
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space(margin);
                    GUILayout.BeginHorizontal();
                    currentWidth = 0;
                }

                DrawPortraitCell(unitID, index, iconSize);
                currentWidth += iconSize + margin;
            }

            // Wrap cho nút Add "+"
            if (currentWidth + iconSize + margin > windowWidth - 40)
            {
                GUILayout.EndHorizontal();
                EditorGUILayout.Space(margin);
                GUILayout.BeginHorizontal();
            }

            DrawAddCell(tier, iconSize);

            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawPortraitCell(string unitID, int index, float size)
        {
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            Texture2D tex = GetOrCreatePortrait(unitID);

            var boxStyle = new GUIStyle("box");
            boxStyle.padding = new RectOffset(2, 2, 2, 2);
            GUI.Box(rect, GUIContent.none, boxStyle);

            if (tex != null)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Label(rect, "?\nNo Img", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            }

            // Nhãn tên màu trắng nền mờ ở dưới cùng góc ảnh
            Rect labelRect = new Rect(rect.x, rect.yMax - 20, rect.width, 20);
            GUI.DrawTexture(labelRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.6f), 0, 0);
            GUI.Label(labelRect, unitID, new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.MiddleCenter });

            // Button ẩn phủ kín diện tích để bắt Click
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                _currentView = ViewMode.EditUnit;
                _editingIndex = index;
                GUI.FocusControl(null);
            }
        }

        private void DrawAddCell(int tier, float size)
        {
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            var boxStyle = new GUIStyle("box");
            GUI.Box(rect, GUIContent.none, boxStyle);

            GUI.Label(rect, "+", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24 });

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                AddNewUnit(tier);
            }
        }

        private void DrawEditView()
        {
            SerializedProperty entriesProp = _soConfig.FindProperty("unitEntries");
            if (_editingIndex < 0 || _editingIndex >= entriesProp.arraySize)
            {
                GUILayout.Label("Invalid selection. Unit may have been deleted.");
                return;
            }

            SerializedProperty elem = entriesProp.GetArrayElementAtIndex(_editingIndex);
            string unitID = elem.FindPropertyRelative("UnitID").stringValue;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Vẽ thẻ hình đại diện siêu to khổng lồ
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Texture2D tex = GetOrCreatePortrait(unitID);
            if (tex != null)
            {
                var boxStyle = new GUIStyle("box");
                GUILayout.BeginVertical(boxStyle, GUILayout.Width(150), GUILayout.Height(150));
                GUILayout.Label(tex, GUILayout.Width(150), GUILayout.Height(150));
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Box("No Image Found", GUILayout.Width(150), GUILayout.Height(150));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Editing: {unitID}", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 }, GUILayout.Width(200), GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(20);

            // Bắt đầu vẽ danh sách các properties con
            EditorGUI.BeginChangeCheck();

            SerializedProperty iter = elem.Copy();
            SerializedProperty end = iter.GetEndProperty();
            iter.NextVisible(true);
            while (!SerializedProperty.EqualContents(iter, end))
            {
                EditorGUILayout.PropertyField(iter, true);
                if (!iter.NextVisible(false))
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                _soConfig.ApplyModifiedProperties();
                // Clear cache just in case we edited UnitID field
                ClearCache();
            }

            EditorGUILayout.Space(30);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete Unit", GUILayout.Width(150), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Unit", $"Are you sure you want to delete {unitID}?", "Yes", "No"))
                {
                    entriesProp.DeleteArrayElementAtIndex(_editingIndex);
                    _soConfig.ApplyModifiedProperties();
                    _currentView = ViewMode.TierList;
                    _editingIndex = -1;
                    ClearCache();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.EndScrollView();
        }

        private void AddNewUnit(int tier)
        {
            SerializedProperty entriesProp = _soConfig.FindProperty("unitEntries");
            int index = entriesProp.arraySize;
            entriesProp.InsertArrayElementAtIndex(index);

            SerializedProperty newElem = entriesProp.GetArrayElementAtIndex(index);
            string prefix = string.IsNullOrEmpty(_prefixFilter) ? "NewUnit_" : _prefixFilter + "New_";
            newElem.FindPropertyRelative("UnitID").stringValue = prefix + index;
            newElem.FindPropertyRelative("Tier").intValue = tier;

            _soConfig.ApplyModifiedProperties();

            _editingIndex = index;
            _currentView = ViewMode.EditUnit;
            GUI.FocusControl(null);
        }

        // =========================================================================
        // Texture Array Extraction Logic
        // =========================================================================

        private Texture2D GetOrCreatePortrait(string unitID)
        {
            if (string.IsNullOrEmpty(unitID)) return null;

            if (_portraitCache.TryGetValue(unitID, out Texture2D cachedTex))
            {
                return cachedTex;
            }

            if (_renderDb == null) return null;

            var profile = _renderDb.GetUnitByID(unitID);
            if (profile?.animData?.textureArray == null)
            {
                _portraitCache[unitID] = null;
                return null;
            }

            var t2dArray = profile.animData.textureArray;
            int sliceIndex = 0; // Mặc định frame số không

            if (profile.animData.GetAnim(UnitAnimState.Idle, out var idleClip))
            {
                sliceIndex = idleClip.startFrame;
            }
            else if (profile.animData.animations.Count > 0)
            {
                sliceIndex = profile.animData.animations[0].startFrame;
            }

            Texture2D extractedTex = ExtractSlice(t2dArray, sliceIndex);
            _portraitCache[unitID] = extractedTex;

            return extractedTex;
        }

        private Texture2D ExtractSlice(Texture2DArray t2dArray, int sliceIndex)
        {
            int width = t2dArray.width;
            int height = t2dArray.height;

            // Use a RenderTexture to safely convert compressed arrays to read/write enabled textures
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture prevActive = RenderTexture.active;

            Material blitMat = null;
            Shader sliceShader = Shader.Find("Hidden/BlitSlice");

            if (sliceShader != null)
            {
                blitMat = new Material(sliceShader);
                if (blitMat.shader.isSupported)
                {
                    blitMat.SetInt("_Slice", sliceIndex);
                    Graphics.Blit(t2dArray, rt, blitMat);
                }
            }

            if (blitMat == null || !blitMat.shader.isSupported)
            {
                // Fallback: If custom shader not available, copytexture still throws on compressed formats.
                // We will try an intermediate uncompressed texture if we must, 
                // but Unity 2021+ supports Graphics.Blit directly from arrays if Z is set, wait, Blit doesn't natively support Z slice without a custom shader.

                // Let's use Graphics.CopyTexture safely by ensuring source and dest format match structurally? No, the error says dst is format 4 (RGBA32) and src is 100 (ASTC / compressed).

                // Unity workaround for extracting a slice from a compressed array:
                // We create an intermediate Texture2D matching the EXACT format of the array.
                Texture2D tempSlice = new Texture2D(width, height, t2dArray.format, t2dArray.mipmapCount > 1);
                Graphics.CopyTexture(t2dArray, sliceIndex, 0, tempSlice, 0, 0);

                // Then Blit that intermediate to the RenderTexture which decompresses it
                Graphics.Blit(tempSlice, rt);

                DestroyImmediate(tempSlice);
            }

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            if (blitMat != null) DestroyImmediate(blitMat);

            return tex;
        }

        private void ClearCache()
        {
            foreach (var kvp in _portraitCache)
            {
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value); // Hủy texture ra khỏi RAM Editor tránh rò rỉ
                }
            }
            _portraitCache.Clear();
        }

        private void OnDestroy()
        {
            ClearCache();
        }

        // =========================================================================
        // Legacy CSV Import Logic ported over
        // =========================================================================

        private void ImportCSV()
        {
            if (_config == null) return;

            string path = EditorUtility.OpenFilePanel("Select Unit Configs CSV", "Assets", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string[] lines;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    var lineList = new List<string>();
                    while (!sr.EndOfStream)
                        lineList.Add(sr.ReadLine());
                    lines = lineList.ToArray();
                }

                _config.unitEntries.Clear();

                int importedCount = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] cols = line.Split(',');
                    if (cols.Length < 11) continue;

                    string id = cols[0];
                    float maxHp = float.Parse(cols[1], CultureInfo.InvariantCulture);
                    float moveSpd = float.Parse(cols[2], CultureInfo.InvariantCulture);
                    float baseDmg = float.Parse(cols[3], CultureInfo.InvariantCulture);
                    float atkCooldown = float.Parse(cols[4], CultureInfo.InvariantCulture);
                    float atkRange = float.Parse(cols[5], CultureInfo.InvariantCulture);
                    float projSpd = float.Parse(cols[6], CultureInfo.InvariantCulture);

                    Enum.TryParse(cols[7], true, out AttackType atkType);
                    Enum.TryParse(cols[8], true, out TargetType tgtType);

                    int buildCost = int.Parse(cols[9]);
                    int tier = int.Parse(cols[10]);

                    var parsedData = new UnitConfig(
                        id, maxHp, moveSpd, baseDmg, atkCooldown, atkRange, projSpd,
                        atkType, tgtType, buildCost, tier
                    );

                    _config.unitEntries.Add(parsedData);
                    importedCount++;
                }

                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                _soConfig.Update();
                ClearCache();

                EditorUtility.DisplayDialog("Import CSV", $"Thành công! Đã nạp {importedCount} Unit Configs.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Import CSV] Lỗi: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Lỗi khi đọc file: {ex.Message}", "OK");
            }
        }
    }
}
