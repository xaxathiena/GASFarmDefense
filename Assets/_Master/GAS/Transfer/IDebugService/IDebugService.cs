using System;
using UnityEngine;

public interface IDebugService
{
    // Log message lên màn hình (thay vì Console của Editor)
    void Log(string message, Color color = default);
    
    // Đăng ký nút Cheat (Ví dụ: "Add 1000 Gold", () => gold += 1000)
    void AddCommand(string name, Action action);
    
    // Bật/Tắt console
    void Toggle();
}