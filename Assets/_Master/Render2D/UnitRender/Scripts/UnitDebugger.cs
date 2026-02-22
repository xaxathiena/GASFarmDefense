using UnityEngine;
using VContainer;
using Abel.TowerDefense.Render;
using Abel.TowerDefense.Data;
using System;

namespace Abel.TowerDefense.DebugTools
{
    public class UnitDebugger : MonoBehaviour
    {
        private GameRenderManager renderManager;
        private UnitRenderGameSettings settings;
        private bool showDebugger = true;

        // Delegate for any external Logic System to hook into and provide extra info
        public Func<int, string> onRequestLogicInfo;

        [Inject]
        public void Construct(GameRenderManager renderMgr, UnitRenderGameSettings settings)
        {
            renderManager = renderMgr;
            this.settings = settings;
        }

        [Header("Settings")]
        public float clickRadius = 1.5f;
        [Header("Settings")]
        public float clickRadiusPixels = 50f; // Đổi sang dùng Pixel trên màn hình (Ví dụ: cách 50 pixel là trúng)
        public float unitBodyHeightOffset = 1.0f; // Bù trừ chiều cao để click trúng giữa thân thay vì dưới chân
        public Color selectionColor = Color.green;

        // Selection State
        private int selectedInstanceID = -1;
        private UnitRenderData cachedRenderData;
        private string cachedLogicInfo = "";
        private string groupName = "";

        void Update()
        {
            if (settings != null && !settings.IsDebugMode) return; // Skip all logic if not in debug mode
            if (Input.GetMouseButtonDown(0))
            {
                SelectUnitUnderMouse();
            }
        }

        private void SelectUnitUnderMouse()
        {
            Vector2 mousePos = Input.mousePosition;
            float minScreenDist = clickRadiusPixels;

            int bestIdx = -1;
            string bestGroupName = "";
            UnitRenderData bestData = default;

            var loadedGroups = renderManager.LoadedRenderGroups;

            // Duyệt qua toàn bộ Render Groups
            foreach (var kvp in loadedGroups)
            {
                var group = kvp.Value;
                var dataArray = group.GetRenderData(); // Lấy trực tiếp mảng NativeArray để xử lý

                for (int i = 0; i < dataArray.Length; i++)
                {
                    var data = dataArray[i];

                    // Bỏ qua các data trống (instanceID = 0)
                    if (data.instanceID == 0) continue;

                    // 1. Lấy vị trí 3D thực tế của Unit (chân của nó)
                    Vector3 worldPos = new Vector3(data.position.x, 0, data.position.y);

                    // 2. Cộng thêm chiều cao bù trừ để điểm neo dời lên giữa thân nhân vật
                    worldPos.y += unitBodyHeightOffset * data.scale;

                    // 3. Chuyển đổi từ tọa độ Thế giới 3D sang tọa độ Màn hình 2D (Pixel)
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

                    // Nếu đối tượng nằm sau lưng Camera thì bỏ qua
                    if (screenPos.z < 0) continue;

                    // 4. Tính toán khoảng cách (bằng Pixel)
                    float dist = Vector2.Distance(mousePos, new Vector2(screenPos.x, screenPos.y));

                    // Lưu lại unit gần con trỏ chuột nhất
                    if (dist < minScreenDist)
                    {
                        minScreenDist = dist;
                        bestIdx = i;
                        bestGroupName = kvp.Key;
                        bestData = data;
                    }
                }
            }

            // Nếu tìm thấy mục tiêu phù hợp
            if (bestIdx != -1)
            {
                SetSelection(bestData, bestGroupName);
            }
            else
            {
                Deselect();
            }
        }
        private void SetSelection(UnitRenderData renderData, string name)
        {
            selectedInstanceID = renderData.instanceID;
            cachedRenderData = renderData;
            groupName = name;

            // Ask whoever is listening (Logic System) to provide the string.
            // If no one is listening, display a default message.
            if (onRequestLogicInfo != null)
            {
                // Iterate through all subscribed systems
                foreach (Func<int, string> handler in onRequestLogicInfo.GetInvocationList())
                {
                    string result = handler.Invoke(selectedInstanceID);
                    if (!string.IsNullOrEmpty(result))
                    {
                        cachedLogicInfo = result;
                        break; // Found the system that owns this ID!
                    }
                }
            }
            else
            {
                cachedLogicInfo = "No Logic System hooked into Debugger.";
            }

            Debug.Log($"Selected {name} [ID: {selectedInstanceID}]");
        }

        private void Deselect()
        {
            selectedInstanceID = -1;
            cachedLogicInfo = "";
        }

        void OnGUI()
        {
            if (selectedInstanceID == -1) return;

            float width = 300;
            float height = 220;
            float x = 20;
            float y = Screen.height - height - 20;

            GUI.Box(new Rect(x, y, width, height), $"DEBUG: {groupName} [ID: {selectedInstanceID}]");

            GUILayout.BeginArea(new Rect(x + 10, y + 25, width - 20, height - 30));

            GUILayout.Label($"<b>--- Render Data ---</b>");
            GUILayout.Label($"Pos: {cachedRenderData.position}");
            GUILayout.Label($"Rot: {cachedRenderData.rotation:F1}°");
            GUILayout.Label($"Anim: Idx {cachedRenderData.animIndex} | Time: {cachedRenderData.animTimer:F2}");

            GUILayout.Space(10);
            GUILayout.Label($"<b>--- Logic Data ---</b>");
            GUILayout.Label(cachedLogicInfo);

            GUILayout.EndArea();
        }

        void OnDrawGizmos()
        {
            if (selectedInstanceID != -1)
            {
                Vector3 pos = new Vector3(cachedRenderData.position.x, 0, cachedRenderData.position.y);
                Gizmos.color = selectionColor;
                Gizmos.DrawWireSphere(pos, 1.0f);
                Gizmos.DrawLine(pos, pos + Vector3.up * 2);
            }
        }
    }
}