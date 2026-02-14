using System;
using UnityEngine;

public interface IDebugService
{
    // Log message lên màn hình (thay vì Console của Editor)
    // logIndex = -1: log mới liên tục (mặc định)
    // logIndex >= 0: thay thế log tại index đó (giống Unreal)
    void Log(string message, Color color = default, int logIndex = -1);
    
    // Đăng ký nút Cheat (Ví dụ: "Add 1000 Gold", () => gold += 1000)
    void AddCommand(string name, Action action);
    
    // Cập nhật battle stats (pass by readonly ref để tránh copy)
    void UpdateBattleStats(in BattleStats stats);
    
    // Cập nhật GAS performance stats
    void UpdateGASStats(in GASPerformanceStats stats);
    
    // Bật/Tắt console
    void Toggle();
}