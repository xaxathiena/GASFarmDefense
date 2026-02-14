/// <summary>
/// Global debug configuration - Static để access nhanh từ mọi nơi
/// Thay đổi EnableDebug = false để tắt toàn bộ debug features
/// </summary>
public static class DebugConfig
{
    /// <summary>
    /// Master switch cho tất cả debug features
    /// false = Tắt hoàn toàn debug console, logs, commands
    /// </summary>
    public static bool EnableDebug = true;
    
    /// <summary>
    /// Hiển thị stats (FPS, RAM, Battle stats)
    /// </summary>
    public static bool ShowStats = true;
    
    /// <summary>
    /// Enable cheat commands
    /// </summary>
    public static bool EnableCheats = true;
    
    /// <summary>
    /// Max số logs giữ lại
    /// </summary>
    public static int MaxLogs = 50;
    
    /// <summary>
    /// Cập nhật stats bao nhiêu giây một lần (thấp hơn = dễ đọc hơn)
    /// </summary>
    public static float StatsUpdateInterval = 0.5f; // 0.5s update once
    
    /// <summary>
    /// Số frame để tính FPS trung bình (smooth FPS)
    /// </summary>
    public static int FPSAverageFrames = 30;
}
