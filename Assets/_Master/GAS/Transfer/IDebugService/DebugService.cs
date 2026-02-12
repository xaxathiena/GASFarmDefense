using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DebugService : IDebugService, IStartable, IDisposable
{
    private DebugView _view;
    private readonly Queue<string> _logs = new Queue<string>();
    private readonly Dictionary<string, Action> _commands = new Dictionary<string, Action>();
    
    private const int MAX_LOGS = 50; // Chỉ giữ 50 dòng log cuối cùng

    public void Start()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Tự tạo GameObject chứa View
        var go = new GameObject("[Debug_Console]");
        _view = go.AddComponent<DebugView>();
        
        // Sync dữ liệu ban đầu
        _view.UpdateData(_logs, _commands);
        
        Log("Debug Service Initialized!", Color.green);
#endif
    }

    public void Log(string message,  Color color = default)
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formatted = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>[{timestamp}] {message}</color>";

        _logs.Enqueue(formatted);
        
        // Xóa log cũ nếu đầy
        if (_logs.Count > MAX_LOGS) _logs.Dequeue();
        
        // Update View (nếu đã tạo)
        if (_view != null) _view.UpdateData(_logs, _commands);
#endif
    }

    public void AddCommand(string name, Action action)
    {
        if (!_commands.ContainsKey(name))
        {
            _commands.Add(name, action);
            if (_view != null) _view.UpdateData(_logs, _commands);
        }
    }

    public void Toggle() => _view?.Toggle();

    public void Dispose()
    {
        if (_view != null) UnityEngine.Object.Destroy(_view.gameObject);
    }
}