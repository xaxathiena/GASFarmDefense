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
    private BattleStats _battleStats;
    private GASPerformanceStats _gasStats;

    public void Start()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (!DebugConfig.EnableDebug) return; // Tắt hoàn toàn nếu flag = false
        
        // Tự tạo GameObject chứa View
        var go = new GameObject("[Debug_Console]");
        _view = go.AddComponent<DebugView>();
        
        // Sync dữ liệu ban đầu
        _view.UpdateData(_logs, _indexedLogs, _commands, in _battleStats, in _gasStats);
        
        Log("Debug Service Initialized!", Color.green);
#endif
    }

    public void Log(string message, Color color = default, int logIndex = -1)
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (!DebugConfig.EnableDebug) return;
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formatted = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>[{timestamp}] {message}</color>";

        if (logIndex == -1)
        {
            // Continuous logs - add new
            _logs.Add(formatted);
            
            // Xóa log cũ nếu đầy
            if (_logs.Count > DebugConfig.MaxLogs) _logs.RemoveAt(0);
        }
        else
        {
            // Indexed logs - replace at index (giống Unreal)
            _indexedLogs[logIndex] = formatted;
        }
        
        // Update View (nếu đã tạo)
        if (_view != null) _view.UpdateData(_logs, _indexedLogs, _commands, in _battleStats, in _gasStats);
#endif
    }

    public void AddCommand(string name, Action action)
    {
        if (!DebugConfig.EnableDebug || !DebugConfig.EnableCheats) return;
        
        if (!_commands.ContainsKey(name))
        {
            _commands.Add(name, action);
            if (_view != null) _view.UpdateData(_logs, _indexedLogs, _commands, in _battleStats, in _gasStats);
        }
    }
    
    public void UpdateBattleStats(in BattleStats stats)
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (!DebugConfig.EnableDebug) return;
        
        _battleStats = stats;
        if (_view != null) _view.UpdateData(_logs, _indexedLogs, _commands, in _battleStats, in _gasStats);
#endif
    }
    
    public void UpdateGASStats(in GASPerformanceStats stats)
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (!DebugConfig.EnableDebug) return;
        
        _gasStats = stats;
        if (_view != null) _view.UpdateData(_logs, _indexedLogs, _commands, in _battleStats, in _gasStats);
#endif
    }

    public void Toggle() => _view?.Toggle();

    public void Dispose()
    {
        if (_view != null) UnityEngine.Object.Destroy(_view.gameObject);
    }
}