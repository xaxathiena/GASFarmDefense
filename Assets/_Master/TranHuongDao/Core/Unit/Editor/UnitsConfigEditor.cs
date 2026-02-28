using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Globalization;
using Abel.TranHuongDao.Core;

namespace Abel.TranHuongDao.EditorTools
{
    [CustomEditor(typeof(UnitsConfig))]
    public class UnitsConfigEditor : Editor
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
                int importedCount = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] cols = line.Split(',');

                    // Phải đảm bảo đủ 11 cột như lúc chúng ta export
                    if (cols.Length < 11)
                    {
                        Debug.LogWarning($"[Import CSV] Bỏ qua dòng {i + 1} vì thiếu dữ liệu.");
                        continue;
                    }

                    // Parse dữ liệu (Dùng InvariantCulture để tránh lỗi dấu phẩy/chấm ở các win khác nhau)
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

                    // Tạo Struct
                    UnitConfigData parsedData = new UnitConfigData(
                        id, maxHp, moveSpd, baseDmg, atkCooldown, atkRange, projSpd,
                        atkType, tgtType, buildCost, tier
                    );

                    // Thêm vào List
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