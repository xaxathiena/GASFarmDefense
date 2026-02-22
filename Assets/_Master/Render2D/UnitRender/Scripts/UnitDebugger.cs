using UnityEngine;
using VContainer;
using Abel.TowerDefense.Render;
using Abel.TowerDefense.Data;
using System;
using Unity.Profiling;

namespace Abel.TowerDefense.DebugTools
{
    public class UnitDebugger : MonoBehaviour
    {
        private GameRenderManager renderManager;
        private UnitRenderGameSettings settings;

        // Delegate for any external Logic System to hook into and provide extra info
        public Func<int, string> onRequestLogicInfo;

        [Inject]
        public void Construct(GameRenderManager renderMgr, UnitRenderGameSettings settings)
        {
            renderManager = renderMgr;
            this.settings = settings;
        }

        [Header("Selection Settings")]
        public float clickRadiusPixels = 50f; 
        public float unitBodyHeightOffset = 1.0f; 
        public Color selectionColor = Color.green;

        // Selection State
        private int selectedInstanceID = -1;
        private UnitRenderData cachedRenderData;
        private string cachedLogicInfo = "";
        private string groupName = "";

        // --- PERFORMANCE STATS STATE ---
        private float fpsUpdateInterval = 0.5f;
        private float fpsAccumulator = 0f;
        private int fpsFrames = 0;
        private float currentFps = 0f;
        private float statsTimer = 0f;
        private int totalDrawnUnits = 0;
        private int totalGroups = 0;

        // Profiler Recorders for deep engine metrics
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder allocRecorder;
        private ProfilerRecorder totalMemRecorder;
        
        // New Recorders for Thread Timings
        private ProfilerRecorder mainThreadRecorder;
        private ProfilerRecorder renderThreadRecorder;

        void OnEnable()
        {
            // Initialize profiler recorders
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            allocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocation In Frame Count");
            totalMemRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            
            // Internal category contains thread execution timings
            mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            renderThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
        }

        void OnDisable()
        {
            // Dispose to prevent memory leaks
            drawCallsRecorder.Dispose();
            allocRecorder.Dispose();
            totalMemRecorder.Dispose();
            mainThreadRecorder.Dispose();
            renderThreadRecorder.Dispose();
        }

        void Update()
        {
            if (settings != null && !settings.IsDebugMode) return; 

            UpdatePerformanceStats();

            if (Input.GetMouseButtonDown(0))
            {
                SelectUnitUnderMouse();
            }
        }

        private void UpdatePerformanceStats()
        {
            fpsAccumulator += Time.unscaledDeltaTime;
            fpsFrames++;
            statsTimer += Time.unscaledDeltaTime;

            // Only update heavy stats text twice a second to avoid UI flickering and CPU overhead
            if (statsTimer >= fpsUpdateInterval)
            {
                currentFps = fpsFrames / fpsAccumulator;
                fpsAccumulator = 0f;
                fpsFrames = 0;
                statsTimer = 0f;

                // Recalculate total units by scanning render data
                totalDrawnUnits = 0;
                totalGroups = 0;
                
                if (renderManager != null && renderManager.LoadedRenderGroups != null)
                {
                    totalGroups = renderManager.LoadedRenderGroups.Count;
                    foreach (var kvp in renderManager.LoadedRenderGroups)
                    {
                        var dataArray = kvp.Value.GetRenderData();
                        for (int i = 0; i < dataArray.Length; i++)
                        {
                            if (dataArray[i].instanceID != 0) totalDrawnUnits++;
                        }
                    }
                }
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

            foreach (var kvp in loadedGroups)
            {
                var group = kvp.Value;
                var dataArray = group.GetRenderData(); 

                for (int i = 0; i < dataArray.Length; i++)
                {
                    if (dataArray[i].instanceID == 0) continue;

                    Vector3 worldPos = new Vector3(dataArray[i].position.x, 0, dataArray[i].position.y);
                    worldPos.y += unitBodyHeightOffset * dataArray[i].scale;
                    
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

                    if (screenPos.z < 0) continue;

                    float dist = Vector2.Distance(mousePos, new Vector2(screenPos.x, screenPos.y));

                    if (dist < minScreenDist)
                    {
                        minScreenDist = dist;
                        bestIdx = i;
                        bestGroupName = kvp.Key;
                        bestData = dataArray[i];
                    }
                }
            }

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

            if (onRequestLogicInfo != null)
            {
                foreach (Func<int, string> handler in onRequestLogicInfo.GetInvocationList())
                {
                    string result = handler.Invoke(selectedInstanceID);
                    if (!string.IsNullOrEmpty(result))
                    {
                        cachedLogicInfo = result;
                        break; 
                    }
                }
            }
            else
            {
                cachedLogicInfo = "No Logic System hooked into Debugger.";
            }
        }

        private void Deselect()
        {
            selectedInstanceID = -1;
            cachedLogicInfo = "";
        }

        void OnGUI()
        {
            if (settings != null && !settings.IsDebugMode) return;

            DrawGeneralStats();
            DrawSelectedUnitInfo();
        }

        private void DrawGeneralStats()
        {
            float width = 280;
            float height = 500; // Tăng chiều cao lên một chút để chứa 2 dòng mới
            float x = 10;
            float y = 10;

            // Create a semi-transparent dark box for readability
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 10, y + 10, width - 20, height - 20));

            // Color formatting for FPS
            string fpsColor = currentFps > 50 ? "green" : (currentFps > 30 ? "yellow" : "red");
            GUILayout.Label($"<b>FPS:</b> <color={fpsColor}>{currentFps:F1}</color>");
            
            // --- NEW: Thread Timings ---
            // Convert nanoseconds to milliseconds (1 ms = 1,000,000 ns)
            float mainThreadMs = mainThreadRecorder.LastValue / 1e6f;
            float renderThreadMs = renderThreadRecorder.LastValue / 1e6f;
            
            string cpuColor = mainThreadMs > 16f ? "red" : "white"; // >16ms = rớt mốc 60FPS
            string renderColor = renderThreadMs > 16f ? "red" : "white";
            
            GUILayout.Label($"<b>CPU Main:</b> <color={cpuColor}>{mainThreadMs:F2} ms</color>");
            GUILayout.Label($"<b>Render Thread:</b> <color={renderColor}>{renderThreadMs:F2} ms</color>");

            GUILayout.Space(2);
            GUILayout.Label($"<b>Draw Calls:</b> {drawCallsRecorder.LastValue}");
            
            // Format memory to MB
            float memMB = totalMemRecorder.LastValue / (1024f * 1024f);
            GUILayout.Label($"<b>Memory Used:</b> {memMB:F2} MB");
            
            // Color formatting for GC Alloc (Ideally it should be 0)
            long gcAlloc = allocRecorder.LastValue;
            string gcColor = gcAlloc > 0 ? "red" : "green";
            GUILayout.Label($"<b>GC Alloc (Frame):</b> <color={gcColor}>{gcAlloc}</color> bytes");

            GUILayout.Space(5);
            GUILayout.Label($"<b>Active Groups:</b> {totalGroups} | <b>Units:</b> {totalDrawnUnits}");

            GUILayout.EndArea();
        }

        private void DrawSelectedUnitInfo()
        {
            if (selectedInstanceID == -1) return;

            float width = 300;
            float height = 220;
            
            // Move to Top-Right corner
            float x = Screen.width - width - 10;
            float y = 10;

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