using UnityEngine;

namespace FD
{
    // Interface cho các class C# muốn dùng Pool (để tự reset dữ liệu)
    public interface IPoolable
    {
        void OnSpawn();   // Gọi khi lấy ra khỏi hồ
        void OnDespawn(); // Gọi khi trả về hồ (Reset data tại đây)
    }

    public interface IPoolManager
    {
        // --- PHẦN 1: UNITY OBJECTS (MonoBehaviour) ---
        T Spawn<T>(T prefab, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component;
        void Despawn<T>(T instance) where T : Component;
        void Prewarm<T>(T prefab, int count) where T : Component;

        // --- PHẦN 2: PURE C# CLASSES (Non-MonoBehaviour) ---
        // Spawn một class thuần C# (được Inject dependency)
        T SpawnClass<T>() where T : class;
        
        // Trả class về hồ
        void DespawnClass<T>(T instance) where T : class;
    }
}