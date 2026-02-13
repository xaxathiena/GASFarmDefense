using UnityEngine;
using System.Collections.Generic;

public class DebugView : MonoBehaviour
{
    // Dữ liệu sẽ được Service bơm vào
    private List<string> _logs = new List<string>(); // Continuous logs
    private Dictionary<int, string> _indexedLogs = new Dictionary<int, string>(); // Indexed logs (Unreal-style)
    private Dictionary<string, System.Action> _commands = new Dictionary<string, System.Action>();
    
    private bool _isVisible = false;
    private Vector2 _scrollPos;
    private float _fps;

    // Cấu hình GUI
    private GUIStyle _style;
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject); // Sống sót qua các scene
        _style = new GUIStyle();
        _style.fontSize = 24; // Chữ to cho dễ nhìn trên mobile
        _style.normal.textColor = Color.white;
    }

    void Update()
    {
        // Tính FPS đơn giản
        _fps = 1.0f / Time.deltaTime;
        
        // Phím tắt bật tắt trên PC (dấu huyền `)
        if (Input.GetKeyDown(KeyCode.BackQuote)) _isVisible = !_isVisible;
        
        // Touch 3 ngón tay để bật trên Mobile
        if (Input.touchCount == 3 && Input.GetTouch(0).phase == TouchPhase.Began) 
            _isVisible = !_isVisible;
    }

    public void UpdateData(List<string> logs, Dictionary<int, string> indexedLogs, Dictionary<string, System.Action> commands)
    {
        _logs = logs;
        _indexedLogs = indexedLogs;
        _commands = commands;
    }

    public void Toggle() => _isVisible = !_isVisible;

    void OnGUI()
    {
        if (!_isVisible) return;

        // Vẽ nền đen mờ
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
        
        // 1. Header Info
        GUILayout.Label($"FPS: {_fps:0.} | RAM: {System.GC.GetTotalMemory(false) / 1048576} MB", _style);
        GUILayout.Space(20);

        // 2. Logs Area (Chiếm 70% màn hình)
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(Screen.height * 0.6f));
        
        // Hiển thị indexed logs trước (sorted by key)
        var sortedIndexes = new List<int>(_indexedLogs.Keys);
        sortedIndexes.Sort();
        foreach (var index in sortedIndexes)
        {
            GUILayout.Label($"[{index}] {_indexedLogs[index]}", _style);
        }
        
        // Sau đó hiển thị continuous logs
        foreach (var log in _logs)
        {
            GUILayout.Label(log, _style);
        }
        GUILayout.EndScrollView();

        GUILayout.Space(20);

        // 3. Cheat Buttons Area
        GUILayout.Label("--- COMMANDS ---", _style);
        foreach (var cmd in _commands)
        {
            if (GUILayout.Button(cmd.Key, GUILayout.Height(50))) // Nút to dễ bấm
            {
                cmd.Value?.Invoke();
            }
        }

        GUILayout.EndArea();
    }
}