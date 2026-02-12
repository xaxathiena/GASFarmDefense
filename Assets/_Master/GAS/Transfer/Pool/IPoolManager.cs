using UnityEngine;
namespace FD
{
    public interface IPoolManager
    {
        // Spawn một bản sao của prefab tại vị trí pos, rotation rot
        T Spawn<T>(T prefab, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component;

        // Trả object về hồ
        void Despawn<T>(T instance) where T : Component;
    }
}