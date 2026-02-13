using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DebugService : IDebugService, IStartable, IDisposable
{
    private DebugView _view;
    private readonly List<string> _logs = new List<string>(); // Continuous logs (index -1)
    private readonly Dictionary<int, string> _indexedLogs = new Dictionary<int, string>(); // Indexed logs
    private readonly Dictionary<string, Action> _commands = new Dictionary<string, Action>();
    
    private const int MAX_LOGS = 50; // Chỉ giữ 50 dòng log cuối cùng

    public void Start()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Tự tạo GameObject chứa View
        var go = new GameObject("[Debug_Console]");
        _view = go.AddComponent<DebugView>();
        
        // Sync dữ liệu ban đầu
        _view.UpdateData(_logs, _indexedLogs, _commands);
        
        Log("Debug Service Initialized!", Color.green);
#endif
    }

    public void Log(string message, Color color = default, int logIndex = -1)
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formatted = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>[{timestamp}] {message}</color>";

        if (logIndex == -1)
        {
            // Continuous logs - add new
            _logs.Add(formatted);
            
            // Xóa log cũ nếu đầy
            if (_logs.Count > MAX_LOGS) _logs.RemoveAt(0);
        }
        else
        {
            // Indexed logs - replace at index (giống Unreal)
            _indexedLogs[logIndex] = formatted;
        }
        
        // Update View (nếu đã tạo)
        if (_view != null) _view.UpdateData(_logs, _indexedLogs, _commands);
#endif
    }

    public void AddCommand(string name, Action action)
    {
        if (!_commands.ContainsKey(name))
        {
            _commands.Add(name, action);
            if (_view != null) _view.UpdateData(_logs, _indexedLogs, _commands);
        }
    }

    public void Toggle() => _view?.Toggle();

    public void Dispose()
    {
        if (_view != null) UnityEngine.Object.Destroy(_view.gameObject);
    }
}