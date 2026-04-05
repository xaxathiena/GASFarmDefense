// using UnityEditor;
// using UnityEditor.Overlays;
// using UnityEngine;

// // Đăng ký Overlay này vào Scene View
// [Overlay(typeof(SceneView), "Abel's Dev Tools", true)]
// public class AbelDevToolsOverlay : IMGUIOverlay
// {
//     private float timeScale = 1.0f;

//     public override void OnGUI()
//     {
//         // Dàn hàng ngang để trông giống một Toolbar
//         GUILayout.BeginHorizontal();

//         // 1. Cụm Time Scale
//         GUILayout.Label("Time:", GUILayout.Width(40));
//         timeScale = GUILayout.HorizontalSlider(timeScale, 0.0f, 5.0f, GUILayout.Width(100));

//         // Nút reset nhanh về tốc độ thường
//         if (GUILayout.Button("1x", EditorStyles.miniButton, GUILayout.Width(25)))
//         {
//             timeScale = 1.0f;
//         }

//         if (Application.isPlaying)
//         {
//             Time.timeScale = timeScale;
//         }

//         GUILayout.Space(10); // Khoảng cách chia vùng

//         // 2. Cụm tiện ích nhanh
//         if (GUILayout.Button("Clear Save", EditorStyles.miniButton, GUILayout.Width(75)))
//         {
//             PlayerPrefs.DeleteAll();
//             PlayerPrefs.Save();
//             Debug.Log("Đã xóa toàn bộ dữ liệu PlayerPrefs!");
//         }

//         if (GUILayout.Button(EditorApplication.isPaused ? "▶ Resume" : "⏸ Pause", EditorStyles.miniButton, GUILayout.Width(70)))
//         {
//             EditorApplication.isPaused = !EditorApplication.isPaused;
//         }

//         GUILayout.EndHorizontal();
//     }
// }