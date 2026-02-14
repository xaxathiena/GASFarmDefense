using UnityEngine;
using System.Collections.Generic;

public readonly struct BattleStats
{
    public readonly int ActiveTowers;
    public readonly int ActiveEnemies;
    public readonly int TotalGameplayEffects;
    public readonly int AbilitiesOnCooldown;
    
    public BattleStats(int towers, int enemies, int effects, int cooldowns)
    {
        ActiveTowers = towers;
        ActiveEnemies = enemies;
        TotalGameplayEffects = effects;
        AbilitiesOnCooldown = cooldowns;
    }
}

public class DebugView : MonoBehaviour
{
    // Dữ liệu sẽ được Service bơm vào
    private List<string> _logs = new List<string>(); // Continuous logs
    private Dictionary<int, string> _indexedLogs = new Dictionary<int, string>(); // Indexed logs (Unreal-style)
    private Dictionary<string, System.Action> _commands = new Dictionary<string, System.Action>();
    private BattleStats _battleStats;
    private GASPerformanceStats _gasStats;
    
    private bool _isVisible = false;
    private bool _showGASDetails = false; // Toggle GAS detail view
    private Vector2 _scrollPos;
    private Vector2 _gasScrollPos;
    private float _fps;
    
    // FPS smoothing
    private float[] _fpsBuffer;
    private int _fpsBufferIndex = 0;
    
    // Update throttling
    private float _lastStatsUpdateTime = 0f;
    private BattleStats _displayBattleStats; // Cached for display
    private GASPerformanceStats _displayGASStats; // Cached for display

    // Cấu hình GUI
    private GUIStyle _style;
    private GUIStyle _goodStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _criticalStyle;
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject); // Sống sót qua các scene
        
        _style = new GUIStyle();
        _style.fontSize = 24;
        _style.normal.textColor = Color.white;
        
        _goodStyle = new GUIStyle(_style);
        _goodStyle.normal.textColor = Color.green;
        
        _warningStyle = new GUIStyle(_style);
        _warningStyle.normal.textColor = Color.yellow;
        
        _criticalStyle = new GUIStyle(_style);
        _criticalStyle.normal.textColor = Color.red;
        
        // Initialize FPS buffer
        _fpsBuffer = new float[DebugConfig.FPSAverageFrames];
        for (int i = 0; i < _fpsBuffer.Length; i++)
        {
            _fpsBuffer[i] = 60f; // Default to 60 FPS
        }
    }

    void Update()
    {
        // Tính FPS với rolling average (smooth)
        float currentFPS = 1.0f / Time.deltaTime;
        _fpsBuffer[_fpsBufferIndex] = currentFPS;
        _fpsBufferIndex = (_fpsBufferIndex + 1) % _fpsBuffer.Length;
        
        // Calculate average FPS
        float sum = 0;
        for (int i = 0; i < _fpsBuffer.Length; i++)
        {
            sum += _fpsBuffer[i];
        }
        _fps = sum / _fpsBuffer.Length;
        
        // Phím tắt bật tắt trên PC (dấu huyền `)
        if (Input.GetKeyDown(KeyCode.BackQuote)) _isVisible = !_isVisible;
        
        // Toggle GAS details with Tab
        if (Input.GetKeyDown(KeyCode.Tab) && _isVisible) _showGASDetails = !_showGASDetails;
        
        // Touch 3 ngón tay để bật trên Mobile
        if (Input.touchCount == 3 && Input.GetTouch(0).phase == TouchPhase.Began) 
            _isVisible = !_isVisible;
    }

    public void UpdateData(List<string> logs, Dictionary<int, string> indexedLogs, Dictionary<string, System.Action> commands, in BattleStats battleStats, in GASPerformanceStats gasStats)
    {
        _logs = logs;
        _indexedLogs = indexedLogs;
        _commands = commands;
        
        // Throttle stats update - chỉ update theo interval
        float currentTime = Time.time;
        if (currentTime - _lastStatsUpdateTime >= DebugConfig.StatsUpdateInterval)
        {
            _battleStats = battleStats;
            _gasStats = gasStats;
            
            // Cache for display
            _displayBattleStats = battleStats;
            _displayGASStats = gasStats;
            
            _lastStatsUpdateTime = currentTime;
        }
    }

    public void Toggle() => _isVisible = !_isVisible;

    void OnGUI()
    {
        if (!_isVisible) return;

        // Vẽ nền đen mờ
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
        
        // 1. Header Info
        if (DebugConfig.ShowStats)
        {
            GUILayout.Label($"FPS: {_fps:0.} | RAM: {System.GC.GetTotalMemory(false) / 1048576} MB", _style);
            GUILayout.Label($"Towers: {_displayBattleStats.ActiveTowers} | Enemies: {_displayBattleStats.ActiveEnemies}", _style);
            
            // GAS Performance with color-coding
            var effectStyle = GetStyleForLevel(_displayGASStats.GetEffectsLevel());
            var tickStyle = GetStyleForLevel(_displayGASStats.GetTickTimeLevel());
            var gcStyle = GetStyleForLevel(_displayGASStats.GetGCLevel());
            
            GUILayout.Label($"[GAS] ASC: {_displayGASStats.TotalASCCount} | Active Effects: {_displayGASStats.TotalActiveEffects}", effectStyle);
            GUILayout.Label($"Tick: {_displayGASStats.TotalASCTickTimeMs:F2}ms | Applied: {_displayGASStats.EffectsAppliedThisFrame} | Removed: {_displayGASStats.EffectsRemovedThisFrame}", tickStyle);
            GUILayout.Label($"Abilities: {_displayGASStats.AbilityActivationsThisFrame} act | {_displayGASStats.TotalAbilitiesOnCooldown} CD | {_displayGASStats.FailedActivations} fail", _style);
            
            // Button to toggle GAS details
            if (GUILayout.Button(_showGASDetails ? "Hide GAS Details [Tab]" : "Show GAS Details [Tab]", GUILayout.Height(40)))
            {
                _showGASDetails = !_showGASDetails;
            }
            
            GUILayout.Space(10);
        }
        
        // 1.5 GAS Details Panel (if enabled)
        if (_showGASDetails && DebugConfig.ShowStats)
        {
            GUILayout.Label("=== GAS DEEP PROFILER ===", _warningStyle);
            _gasScrollPos = GUILayout.BeginScrollView(_gasScrollPos, GUILayout.Height(Screen.height * 0.3f));
            
            GUILayout.Label($"Total Granted Abilities: {_displayGASStats.TotalGrantedAbilities}", _style);
            GUILayout.Label($"Attribute Mods: {_displayGASStats.AttributeModsThisFrame} | Callbacks: {_displayGASStats.AttributeCallbacksTriggered}", _style);
            GUILayout.Label($"Periodic Ticks: {_displayGASStats.PeriodicEffectTicks}", _style);
            
            if (_displayGASStats.TopSlowEntities != null && _displayGASStats.TopSlowEntities.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("--- TOP 5 SLOWEST ENTITIES ---", _warningStyle);
                
                foreach (var entity in _displayGASStats.TopSlowEntities)
                {
                    var entityStyle = entity.TickTimeMs > 1f ? _criticalStyle : 
                                     entity.TickTimeMs > 0.5f ? _warningStyle : _goodStyle;
                    
                    GUILayout.Label($"[{entity.EntityType}] {entity.EntityName}: {entity.TickTimeMs:F3}ms | {entity.ActiveEffects} FX | {entity.AbilitiesOnCD} CD", entityStyle);
                }
            }
            
            GUILayout.EndScrollView();
            GUILayout.Space(10);
        }

        // 2. Logs Area
        float logHeight = _showGASDetails ? Screen.height * 0.3f : Screen.height * 0.6f;
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(logHeight));
        
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
        if (DebugConfig.EnableCheats && _commands.Count > 0)
        {
            GUILayout.Label("--- COMMANDS ---", _style);
            foreach (var cmd in _commands)
            {
                if (GUILayout.Button(cmd.Key, GUILayout.Height(50))) // Nút to dễ bấm
                {
                    cmd.Value?.Invoke();
                }
            }
        }

        GUILayout.EndArea();
    }
    
    private GUIStyle GetStyleForLevel(GASPerformanceStats.PerformanceLevel level)
    {
        switch (level)
        {
            case GASPerformanceStats.PerformanceLevel.Good:
                return _goodStyle;
            case GASPerformanceStats.PerformanceLevel.Warning:
                return _warningStyle;
            case GASPerformanceStats.PerformanceLevel.Critical:
                return _criticalStyle;
            default:
                return _style;
        }
    }
}