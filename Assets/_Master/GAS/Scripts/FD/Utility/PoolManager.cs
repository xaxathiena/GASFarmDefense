using System.Collections.Generic;
using UnityEngine;

namespace FD.Core
{
    /// <summary>
    /// High-performance pooling facade mirroring Unity's Instantiate/Destroy signature.
    /// </summary>
    public static class PoolManager
    {
        private sealed class PoolBucket
        {
            public readonly GameObject Prefab;
            public readonly Queue<GameObject> Inactive = new();
            public readonly Transform StorageRoot;

            public PoolBucket(GameObject prefab, Transform parent)
            {
                Prefab = prefab;
                StorageRoot = new GameObject($"[Pool] {prefab.name}").transform;
                StorageRoot.SetParent(parent, false);
                StorageRoot.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        private sealed class InstanceHandle
        {
            public readonly PoolBucket Bucket;
            public bool IsInPool;

            public InstanceHandle(PoolBucket bucket)
            {
                Bucket = bucket;
            }
        }

        private static readonly Dictionary<GameObject, PoolBucket> Pools = new();
        private static readonly Dictionary<int, InstanceHandle> InstanceHandles = new();
        private static Transform poolRoot;

        public static GameObject Instantiate(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("PoolManager.Instantiate called with null prefab");
                return null;
            }
            return Instantiate(prefab, Vector3.zero, Quaternion.identity, null);
        }

        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            if (prefab == null)
            {
                Debug.LogWarning("PoolManager.Instantiate called with null prefab");
                return null;
            }

            return Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogWarning("PoolManager.Instantiate called with null prefab");
                return null;
            }

            var bucket = GetPool(prefab);
            GameObject instance = bucket.Inactive.Count > 0 ? bucket.Inactive.Dequeue() : CreateInstance(bucket);
            var handle = InstanceHandles[instance.GetInstanceID()];
            handle.IsInPool = false;

            var transform = instance.transform;
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public static T Instantiate<T>(T prefab) where T : Component
        {
            var instance = Instantiate(prefab.gameObject);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(T prefab, Transform parent) where T : Component
        {
            var instance = Instantiate(prefab.gameObject, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var instance = Instantiate(prefab.gameObject, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static void Destroy(Component component)
        {
            if (component != null)
            {
                Destroy(component.gameObject);
            }
        }

        public static void Destroy(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!TryReturnToPool(instance))
            {
                Object.Destroy(instance);
            }
        }

        private static PoolBucket GetPool(GameObject prefab)
        {
            if (!Pools.TryGetValue(prefab, out var bucket))
            {
                bucket = new PoolBucket(prefab, GetPoolRoot());
                Pools[prefab] = bucket;
            }

            return bucket;
        }

        private static GameObject CreateInstance(PoolBucket bucket)
        {
            var instance = Object.Instantiate(bucket.Prefab);
            RegisterInstance(instance, bucket);
            return instance;
        }

        private static void RegisterInstance(GameObject instance, PoolBucket bucket)
        {
            InstanceHandles[instance.GetInstanceID()] = new InstanceHandle(bucket);
        }

        private static bool TryReturnToPool(GameObject instance)
        {
            if (!InstanceHandles.TryGetValue(instance.GetInstanceID(), out var handle))
            {
                return false;
            }

            if (handle.IsInPool)
            {
                return true;
            }

            handle.IsInPool = true;
            var bucket = handle.Bucket;
            var transform = instance.transform;
            transform.SetParent(bucket.StorageRoot, false);
            instance.SetActive(false);
            bucket.Inactive.Enqueue(instance);
            return true;
        }

        private static Transform GetPoolRoot()
        {
            if (poolRoot == null)
            {
                var rootObject = new GameObject("[PoolManager]");
                Object.DontDestroyOnLoad(rootObject);
                poolRoot = rootObject.transform;
                poolRoot.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }

            return poolRoot;
        }
    }
}
