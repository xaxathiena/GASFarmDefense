using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool; // Thư viện Pool chuẩn của Unity 2021+
using VContainer;
using VContainer.Unity;
namespace FD
{
    public class PoolManager : IPoolManager
    {
        readonly IObjectResolver _resolver;
        readonly Transform _root; // Một GameObject cha để gom tất cả pool vào cho gọn Scene

        // Dictionary map từ "Prefab ID" sang "Object Pool tương ứng"
        // Key: InstanceID của Prefab (int)
        // Value: IObjectPool (Interface chung của các pool)
        private Dictionary<int, object> _pools = new Dictionary<int, object>();

        // Dictionary phụ để biết 1 instance đang thuộc về pool nào (để Despawn cho đúng)
        private Dictionary<int, int> _instanceToPrefabID = new Dictionary<int, int>();

        public PoolManager(IObjectResolver resolver)
        {
            _resolver = resolver;
            // Tạo một object rỗng trong scene để chứa các object được spawn
            _root = new GameObject("[Pool_Root]").transform;
            Object.DontDestroyOnLoad(_root);
        }

        public T Spawn<T>(T prefab, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
        {
            int prefabID = prefab.gameObject.GetInstanceID();

            // 1. Nếu chưa có pool cho prefab này thì tạo mới
            if (!_pools.ContainsKey(prefabID))
            {
                _pools[prefabID] = CreateNewPool(prefab);
            }

            // 2. Lấy Pool ra và xin object
            var pool = (ObjectPool<T>)_pools[prefabID];
            var instance = pool.Get();

            // 3. Setup vị trí
            instance.transform.SetPositionAndRotation(pos, rot);
            instance.transform.SetParent(parent);

            // 4. Lưu dấu vết để sau này Despawn
            _instanceToPrefabID[instance.gameObject.GetInstanceID()] = prefabID;

            return instance;
        }

        public void Despawn<T>(T instance) where T : Component
        {
            int instanceID = instance.gameObject.GetInstanceID();

            // Tìm xem object này thuộc pool nào
            if (_instanceToPrefabID.TryGetValue(instanceID, out int prefabID))
            {
                if (_pools.TryGetValue(prefabID, out object poolObj))
                {
                    var pool = (ObjectPool<T>)poolObj;
                    pool.Release(instance); // Trả về hồ
                    return;
                }
            }

            // Nếu không tìm thấy pool (lỗi logic), thì destroy luôn cho sạch
            Object.Destroy(instance.gameObject);
        }

        // Hàm tạo Pool mới chuẩn VContainer
        private ObjectPool<T> CreateNewPool<T>(T prefab) where T : Component
        {
            return new ObjectPool<T>(
                createFunc: () =>
                {
                    // QUAN TRỌNG NHẤT: Dùng VContainer để tạo object
                    // Để object sinh ra được Inject đầy đủ Dependency
                    return _resolver.Instantiate(prefab, _root);
                },
                actionOnGet: (obj) => obj.gameObject.SetActive(true),
                actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                actionOnDestroy: (obj) => Object.Destroy(obj.gameObject),
                defaultCapacity: 10,
                maxSize: 100
            );
        }
    }
}