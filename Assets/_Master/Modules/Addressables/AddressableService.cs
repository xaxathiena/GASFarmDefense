using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace GASFarmDefense.Modules.Addressables
{
    public interface IAddressableService
    {
        UniTask<T> LoadAssetAsync<T>(object key) where T : UnityEngine.Object;
        UniTask<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false);
        UniTask<SceneInstance> LoadSceneAsync(object key, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true);
        UniTask UnloadSceneAsync(SceneInstance sceneInstance);
        void Release(object assetOrHandle);
        UniTask<IList<T>> LoadAssetsByLabelAsync<T>(string label, Action<T> callback = null) where T : UnityEngine.Object;
    }

    public class AddressableService : IAddressableService, IDisposable
    {
        // Tracks all active handles to ensure they can be released when the service is disposed
        private readonly List<AsyncOperationHandle> _trackedHandles = new List<AsyncOperationHandle>();
        private readonly Dictionary<object, AsyncOperationHandle> _assetToHandleDict = new Dictionary<object, AsyncOperationHandle>();

        public async UniTask<T> LoadAssetAsync<T>(object key) where T : UnityEngine.Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(key);
            _trackedHandles.Add(handle);


            T asset = await handle.ToUniTask();


            if (asset != null && !_assetToHandleDict.ContainsKey(asset))
            {
                _assetToHandleDict[asset] = handle;
            }

            return asset;
        }

        public async UniTask<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace);
            _trackedHandles.Add(handle);


            GameObject go = await handle.ToUniTask();


            if (go != null)
            {
                _assetToHandleDict[go] = handle;
            }

            return go;
        }

        public async UniTask<SceneInstance> LoadSceneAsync(object key, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(key, mode, activateOnLoad);
            _trackedHandles.Add(handle);


            return await handle.ToUniTask();
        }

        public async UniTask UnloadSceneAsync(SceneInstance sceneInstance)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(sceneInstance);
            await handle.ToUniTask();
        }

        public async UniTask<IList<T>> LoadAssetsByLabelAsync<T>(string label, Action<T> callback = null) where T : UnityEngine.Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<T>(label, callback);
            _trackedHandles.Add(handle);


            return await handle.ToUniTask();
        }

        public void Release(object assetOrHandle)
        {
            if (assetOrHandle == null) return;

            // If it's a direct handle
            if (assetOrHandle is AsyncOperationHandle handle)
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                    _trackedHandles.Remove(handle);
                }
                return;
            }

            // If it's an asset or GameObject we tracked
            if (_assetToHandleDict.TryGetValue(assetOrHandle, out AsyncOperationHandle trackedHandle))
            {
                if (trackedHandle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(trackedHandle);
                    _trackedHandles.Remove(trackedHandle);
                }
                _assetToHandleDict.Remove(assetOrHandle);
            }
            else
            {
                // Fallback: try to release directly (works for some types if managed by Addressables internally)
                UnityEngine.AddressableAssets.Addressables.Release(assetOrHandle);
            }
        }

        public void Dispose()
        {
            foreach (var handle in _trackedHandles)
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }
            }
            _trackedHandles.Clear();
            _assetToHandleDict.Clear();
        }
    }
}
