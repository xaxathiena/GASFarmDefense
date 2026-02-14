using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace FD
{
    public class PoolManager : IPoolManager
    {
        readonly IObjectResolver _resolver;
        readonly Transform _root;

        // --- KHO 1: UNITY POOLS ---
        // Key: Prefab InstanceID (int)
        private Dictionary<int, object> _unityPools = new Dictionary<int, object>();
        private Dictionary<int, int> _instanceToPrefabID = new Dictionary<int, int>();

        // --- KHO 2: C# CLASS POOLS ---
        // Key: System.Type (Loại class)
        private Dictionary<Type, object> _classPools = new Dictionary<Type, object>();

        public PoolManager(IObjectResolver resolver)
        {
            _resolver = resolver;
            _root = new GameObject("[Pool_Root]").transform;
            UnityEngine.Object.DontDestroyOnLoad(_root);
        }

        #region --- UNITY OBJECTS SECTION ---

        public T Spawn<T>(T prefab, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
        {
            int prefabID = prefab.gameObject.GetInstanceID();

            if (!_unityPools.ContainsKey(prefabID))
            {
                _unityPools[prefabID] = CreateUnityPool(prefab);
            }

            var pool = (ObjectPool<T>)_unityPools[prefabID];
            var instance = pool.Get();

            // Setup Transform
            instance.transform.SetPositionAndRotation(pos, rot);
            instance.transform.SetParent(parent);
            
            // Map instance ID ngược lại prefab ID để phục vụ Despawn
            _instanceToPrefabID[instance.gameObject.GetInstanceID()] = prefabID;

            return instance;
        }

        public void Despawn<T>(T instance) where T : Component
        {
            if (instance == null) return;

            int instanceID = instance.gameObject.GetInstanceID();

            if (_instanceToPrefabID.TryGetValue(instanceID, out int prefabID))
            {
                if (_unityPools.TryGetValue(prefabID, out object poolObj))
                {
                    var pool = (ObjectPool<T>)poolObj;
                    pool.Release(instance);
                    return;
                }
            }
            
            // Fallback: Nếu không thuộc pool nào thì destroy thường
            UnityEngine.Object.Destroy(instance.gameObject);
        }

        public void Prewarm<T>(T prefab, int count) where T : Component
        {
             int prefabID = prefab.gameObject.GetInstanceID();
             if (!_unityPools.ContainsKey(prefabID)) _unityPools[prefabID] = CreateUnityPool(prefab);
             
             var pool = (ObjectPool<T>)_unityPools[prefabID];
             var temp = new List<T>(count);
             for(int i=0; i<count; i++) temp.Add(pool.Get());
             foreach(var item in temp) pool.Release(item);
        }

        private ObjectPool<T> CreateUnityPool<T>(T prefab) where T : Component
        {
            return new ObjectPool<T>(
                createFunc: () => _resolver.Instantiate(prefab, _root), // VContainer Inject
                actionOnGet: (obj) => obj.gameObject.SetActive(true),
                actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                actionOnDestroy: (obj) => UnityEngine.Object.Destroy(obj.gameObject),
                defaultCapacity: 10,
                maxSize: 100
            );
        }

        #endregion

        #region --- PURE C# CLASSES SECTION ---

        public T SpawnClass<T>() where T : class
        {
            var type = typeof(T);

            if (!_classPools.ContainsKey(type))
            {
                _classPools[type] = CreateClassPool<T>();
            }

            var pool = (ObjectPool<T>)_classPools[type];
            return pool.Get();
        }

        public void DespawnClass<T>(T instance) where T : class
        {
            if (instance == null) return;
            
            var type = typeof(T);
            if (_classPools.TryGetValue(type, out object poolObj))
            {
                var pool = (ObjectPool<T>)poolObj;
                pool.Release(instance);
            }
        }

        private ObjectPool<T> CreateClassPool<T>() where T : class
        {
            return new ObjectPool<T>(
                // 1. Tạo mới: Dùng VContainer để nó tự Inject Dependencies nếu có
                createFunc: () => _resolver.Resolve<T>(), 

                // 2. Lấy ra: Gọi OnSpawn nếu có cài đặt interface
                actionOnGet: (obj) => 
                {
                    if (obj is IPoolable p) p.OnSpawn();
                },

                // 3. Trả về: QUAN TRỌNG - Gọi OnDespawn để Reset data
                actionOnRelease: (obj) => 
                {
                    if (obj is IPoolable p) p.OnDespawn();
                },
                
                // 4. Hủy pool: Dispose nếu có
                actionOnDestroy: (obj) => 
                {
                    if (obj is IDisposable d) d.Dispose();
                },
                defaultCapacity: 20,
                maxSize: 200
            );
        }

        #endregion
    }
}