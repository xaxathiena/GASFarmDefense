using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace FD.Tests
{
    /// <summary>
    /// Advanced performance monitor với detailed metrics
    /// </summary>
    public class AdvancedPerformanceMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showDetailedStats = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private int fontSize = 14;
        
        [Header("Performance Thresholds")]
        [SerializeField] private float warningFPS = 45f;
        [SerializeField] private float criticalFPS = 30f;
        
        // FPS tracking
        private float deltaTime;
        private float fps;
        private float minFPS = float.MaxValue;
        private float maxFPS = 0f;
        private List<float> fpsHistory = new List<float>(100);
        
        // Frame time tracking
        private float avgFrameTime;
        private float maxFrameTime;
        
        // Memory tracking
        private long lastTotalMemory;
        private long lastGCMemory;
        private int lastGCCount;
        
        private GUIStyle labelStyle;
        private GUIStyle warningStyle;
        private GUIStyle criticalStyle;
        private StringBuilder sb = new StringBuilder(1000);

        private void Start()
        {
            Application.targetFrameRate = -1; // Unlimited FPS for testing
            InitStyles();
        }

        private void Update()
        {
            // Calculate FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1.0f / deltaTime;
            
            // Track min/max
            if (fps < minFPS) minFPS = fps;
            if (fps > maxFPS) maxFPS = fps;
            
            // Keep FPS history
            fpsHistory.Add(fps);
            if (fpsHistory.Count > 100)
            {
                fpsHistory.RemoveAt(0);
            }
            
            // Calculate average frame time
            avgFrameTime = deltaTime * 1000f;
            if (avgFrameTime > maxFrameTime)
            {
                maxFrameTime = avgFrameTime;
            }
            
            // Toggle display
            if (Input.GetKeyDown(toggleKey))
            {
                showDetailedStats = !showDetailedStats;
            }
        }

        private void OnGUI()
        {
            if (!showDetailedStats) return;
            
            if (labelStyle == null)
            {
                InitStyles();
            }
            
            // Choose style based on FPS
            GUIStyle currentStyle = labelStyle;
            if (fps < criticalFPS)
            {
                currentStyle = criticalStyle;
            }
            else if (fps < warningFPS)
            {
                currentStyle = warningStyle;
            }
            
            // Build stats string
            sb.Clear();
            sb.AppendLine($"=== PERFORMANCE STATS ===");
            sb.AppendLine($"FPS: {fps:F1} (Min: {minFPS:F1}, Max: {maxFPS:F1})");
            sb.AppendLine($"Frame Time: {avgFrameTime:F2}ms (Max: {maxFrameTime:F2}ms)");
            sb.AppendLine($"Target: 16.67ms for 60 FPS");
            sb.AppendLine();
            
            // Scene stats
            var towers = FindObjectsOfType<FD.Character.TowerBase>();
            var enemies = FindObjectsOfType<FD.Character.FDEnemyBase>();
            
            sb.AppendLine($"=== SCENE STATS ===");
            sb.AppendLine($"Towers: {towers.Length}");
            sb.AppendLine($"Enemies: {enemies.Length}");
            sb.AppendLine();
            
            // Memory stats
            long totalMemory = System.GC.GetTotalMemory(false) / 1024 / 1024;
            int gcCount = System.GC.CollectionCount(0);
            
            sb.AppendLine($"=== MEMORY ===");
            sb.AppendLine($"Total: {totalMemory}MB");
            sb.AppendLine($"GC Collections: {gcCount}");
            if (gcCount > lastGCCount)
            {
                sb.AppendLine($"<color=red>⚠ GC occurred!</color>");
            }
            sb.AppendLine();
            
            // Performance warnings
            if (fps < criticalFPS)
            {
                sb.AppendLine($"🔴 CRITICAL: FPS too low!");
                sb.AppendLine($"Recommended actions:");
                sb.AppendLine($"- Reduce tower count");
                sb.AppendLine($"- Reduce enemy spawn rate");
                sb.AppendLine($"- Check Profiler for bottlenecks");
            }
            else if (fps < warningFPS)
            {
                sb.AppendLine($"⚠ WARNING: FPS below target");
                sb.AppendLine($"Consider optimization");
            }
            else
            {
                sb.AppendLine($"✅ Performance: Good");
            }
            
            sb.AppendLine();
            sb.AppendLine($"Press {toggleKey} to toggle stats");
            
            // Draw background box
            float width = 400;
            float height = 450;
            GUI.Box(new Rect(10, 10, width, height), "");
            
            // Draw text
            GUI.Label(new Rect(20, 20, width - 20, height - 20), sb.ToString(), currentStyle);
            
            // Draw FPS graph
            DrawFPSGraph(new Rect(10, height + 20, width, 100));
            
            // Update last values
            lastTotalMemory = totalMemory;
            lastGCCount = gcCount;
        }

        private void DrawFPSGraph(Rect rect)
        {
            if (fpsHistory.Count < 2) return;
            
            GUI.Box(rect, "FPS History (last 100 frames)");
            
            // Draw graph lines
            Vector2 prevPoint = Vector2.zero;
            float xStep = rect.width / 100f;
            float yScale = rect.height / 120f; // 0-120 FPS range
            
            for (int i = 0; i < fpsHistory.Count; i++)
            {
                float x = rect.x + i * xStep;
                float y = rect.y + rect.height - (fpsHistory[i] * yScale);
                Vector2 point = new Vector2(x, y);
                
                if (i > 0)
                {
                    // Draw line segment (approximation with GUI.Box)
                    Color color = fpsHistory[i] >= warningFPS ? Color.green : 
                                  fpsHistory[i] >= criticalFPS ? Color.yellow : Color.red;
                    
                    DrawLine(prevPoint, point, color);
                }
                
                prevPoint = point;
            }
            
            // Draw reference lines
            float target60 = rect.y + rect.height - (60f * yScale);
            DrawLine(new Vector2(rect.x, target60), new Vector2(rect.x + rect.width, target60), Color.cyan);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            // Simple line drawing using GUI.Box
            Vector2 diff = end - start;
            float length = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);
            
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.Box(new Rect(start.x, start.y - 1, length, 2), GUIContent.none);
            GUI.color = oldColor;
            
            GUI.matrix = matrix;
        }

        private void InitStyles()
        {
            labelStyle = new GUIStyle();
            labelStyle.fontSize = fontSize;
            labelStyle.normal.textColor = Color.white;
            labelStyle.wordWrap = false;
            
            warningStyle = new GUIStyle(labelStyle);
            warningStyle.normal.textColor = Color.yellow;
            
            criticalStyle = new GUIStyle(labelStyle);
            criticalStyle.normal.textColor = Color.red;
        }

        public void ResetStats()
        {
            minFPS = float.MaxValue;
            maxFPS = 0f;
            maxFrameTime = 0f;
            fpsHistory.Clear();
        }

        // Public methods để script khác có thể query
        public float GetCurrentFPS() => fps;
        public float GetMinFPS() => minFPS;
        public float GetMaxFPS() => maxFPS;
        public float GetAvgFrameTime() => avgFrameTime;
        public bool IsPerformanceGood() => fps >= warningFPS;
    }
}
