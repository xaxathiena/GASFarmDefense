using UnityEditor;
using UnityEngine;
using UnityToolbarExtender; // Yêu cầu plugin ở Bước 2

[InitializeOnLoad]
public class MainToolbarTools
{
    static float timeScale = 1.0f;

    static MainToolbarTools()
    {
        // Gắn giao diện vào khu vực bên phải cụm nút Play (chính xác chỗ anh khoanh đỏ)
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {
        // Tạo khoảng cách với cụm nút Play
        GUILayout.Space(15);

        // 1. Cụm Time Scale
        GUILayout.Label("Time:", GUILayout.Width(35));

        // Thanh trượt tốc độ game
        timeScale = EditorGUILayout.Slider(timeScale, 0f, 5f, GUILayout.Width(120));

        // Nút reset nhanh
        if (GUILayout.Button("1x", EditorStyles.miniButton, GUILayout.Width(25)))
        {
            timeScale = 1f;
        }

        // Kiểm soát thời gian khi game đang chạy
        if (Application.isPlaying)
        {
            Time.timeScale = timeScale;
        }

        GUILayout.Space(20);

        // 2. Cụm tiện ích phụ
        if (GUILayout.Button("Clear Save", EditorStyles.miniButton, GUILayout.Width(75)))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("PlayerPrefs đã được dọn sạch.");
        }

        // Đẩy các icon mặc định của Unity (như nút Collaborate, Layout) sang sát lề phải
        GUILayout.FlexibleSpace();
    }
}