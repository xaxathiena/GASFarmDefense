using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Globalization;
using System.Collections.Generic;
using Abel.TranHuongDao.Core;
using FD.Ability;

namespace Abel.TranHuongDao.EditorTools
{
    [CustomEditor(typeof(UnitsConfig))]
    public class UnitsConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Vẽ giao diện mặc định của ScriptableObject
            DrawDefaultInspector();

            UnitsConfig configAsset = (UnitsConfig)target;

            GUILayout.Space(20);

            // Nút bấm Import to đùng
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Import from CSV", GUILayout.Height(35)))
            {
                ImportCSV(configAsset);
            }
            GUI.backgroundColor = Color.white;
        }

        private void ImportCSV(UnitsConfig targetAsset)
        {
            // Mở cửa sổ chọn file
            string path = EditorUtility.OpenFilePanel("Select Unit Configs CSV", "Assets", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                // Open with FileShare.ReadWrite so the import works even when the
                // CSV is already open in Excel or another process.
                string[] lines;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    var lineList = new System.Collections.Generic.List<string>();
                    while (!sr.EndOfStream)
                        lineList.Add(sr.ReadLine());
                    lines = lineList.ToArray();
                }

                // Clear data cũ trước khi chép data mới vào
                targetAsset.unitEntries.Clear();

                // Bỏ qua dòng số 0 (Dòng tiêu đề - Header)
                // Header mapping
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (lines.Length > 0)
                {
                    string[] headers = lines[0].Split(new[] { ',', '\t' });
                    for (int j = 0; j < headers.Length; j++)
                    {
                        string h = headers[j].Trim().Replace(" ", "").ToLower();
                        if (!headerMap.ContainsKey(h)) headerMap[h] = j;
                    }
                }

                // Helper to get value securely
                string GetVal(string[] cols, string headerName)
                {
                    string cleanHeader = headerName.Replace(" ", "").ToLower();
                    if (headerMap.TryGetValue(cleanHeader, out int index) && index < cols.Length)
                        return cols[index].Trim();
                    return "";
                }

                int importedCount = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] cols = line.Split(new[] { ',', '\t' });
                    if (cols.Length < 5) continue; // Min valid check

                    // Parse dữ liệu
                    string id = GetVal(cols, "UnitID");
                    string renderID = GetVal(cols, "UnitRenderID");
                    if (string.IsNullOrEmpty(renderID)) renderID = id;

                    float.TryParse(GetVal(cols, "ScaleFactor"), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale);
                    if (scale <= 0) scale = 1.0f;

                    int.TryParse(GetVal(cols, "Tier"), out int tier);
                    float.TryParse(GetVal(cols, "MaxHealth"), NumberStyles.Float, CultureInfo.InvariantCulture, out float maxHp);
                    float.TryParse(GetVal(cols, "MoveSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out float moveSpd);
                    float.TryParse(GetVal(cols, "BaseDamage"), NumberStyles.Float, CultureInfo.InvariantCulture, out float baseDmg);
                    float.TryParse(GetVal(cols, "ROF"), NumberStyles.Float, CultureInfo.InvariantCulture, out float rof);
                    float.TryParse(GetVal(cols, "AttackRange"), NumberStyles.Float, CultureInfo.InvariantCulture, out float atkRange);
                    float.TryParse(GetVal(cols, "ProjectileSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out float projSpd);

                    string rawAtkType = GetVal(cols, "AttackType");
                    Enum.TryParse(rawAtkType, true, out AttackType atkType);

                    string rawTgtType = GetVal(cols, "TargetType");
                    TargetType tgtType = TargetType.Both;
                    if (rawTgtType.Equals("Everything", StringComparison.OrdinalIgnoreCase))
                    {
                        tgtType = TargetType.Both;
                    }
                    else
                    {
                        Enum.TryParse(rawTgtType, true, out tgtType);
                    }

                    int.TryParse(GetVal(cols, "BuildCost"), out int buildCost);
                    int.TryParse(GetVal(cols, "Armor"), out int armor);
                    
                    // Support both "Armor Type" and "Armor Typ"
                    string rawArmorType = GetVal(cols, "ArmorType");
                    if (string.IsNullOrEmpty(rawArmorType)) rawArmorType = GetVal(cols, "ArmorTyp");
                    Enum.TryParse(rawArmorType, true, out EArmorType armType);

                    string atkAbility = GetVal(cols, "AttackAbilityID");
                    string skillAbility = GetVal(cols, "SkillAbilityID");

                    // Tạo Struct
                    UnitConfig parsedData = new UnitConfig(
                        id, maxHp, moveSpd, baseDmg, rof, atkRange, projSpd,
                        atkType, tgtType, armor, buildCost, tier,
                        armType,
                        atkAbility, skillAbility, renderID, scale
                    );

                    targetAsset.unitEntries.Add(parsedData);
                    importedCount++;
                }

                // Lưu lại các thay đổi vào Asset
                EditorUtility.SetDirty(targetAsset);
                AssetDatabase.SaveAssets();

                Debug.Log($"[Import CSV] Thành công! Đã nạp {importedCount} Unit Configs vào ScriptableObject.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Import CSV] Lỗi khi đọc file: {ex.Message}");
            }
        }
    }
}