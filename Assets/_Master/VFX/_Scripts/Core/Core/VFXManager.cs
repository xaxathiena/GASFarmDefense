using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Effekseer;

namespace FD.Modules.VFX
{
    public class VFXManager : IVFXManager, ITickable, IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly Abel.TranHuongDao.Core.IConfigService _configService;
        private VFXConfigSO _configSO;

        private int _nextHandleID = 1;

        // Native Array để phục vụ chạy vòng lặp hiệu năng cao cho việc dọn dẹp ID nhanh chóng nếu cần
        private NativeList<int> _activeHandleIDs;

        // Dictionary map ID nội bộ với Handle thực tế trên C++ của Effekseer
        // Cần truy cập qua MainThread để kiểm tra tính hợp lệ của vòng đời 
        private Dictionary<int, EffekseerHandle> _handleMap;
        private Dictionary<int, float> _customDurations;

        public VFXManager(IObjectResolver resolver, Abel.TranHuongDao.Core.IConfigService configService)
        {
            _resolver = resolver;
            _configService = configService;

            _activeHandleIDs = new NativeList<int>(256, Allocator.Persistent);
            _handleMap = new Dictionary<int, EffekseerHandle>(256);
            _customDurations = new Dictionary<int, float>(64);
        }

        private VFXConfigSO Config => _configSO ??= _configService.GetConfig<VFXConfigSO>();

        public int PlayEffectAt(string vfxID, Vector3 position)
        {
            if (Config == null)
            {
                Debug.LogWarning("[VFXManager] VFXConfigSO is missing.");
                return -1;
            }

            if (!Config.TryGetConfig(vfxID, out VFXConfigData configData))
            {
                Debug.LogWarning($"[VFXManager] VfxID '{vfxID}' not found in VFXConfigSO!");
                return -1;
            }

            if (configData.EffectAsset == null)
            {
                Debug.LogWarning($"[VFXManager] VfxID '{vfxID}' has no EffectAsset assigned.");
                return -1;
            }

            // Gọi chạy qua C++ plugin của Effekseer
            EffekseerHandle handle = EffekseerSystem.PlayEffect(configData.EffectAsset, position);

            // Gán các thông số từ Config
            handle.SetScale(new Vector3(configData.Scale, configData.Scale, configData.Scale));
            handle.speed = configData.Speed;

            // Tham số vòng lặp không chỉnh trực tiếp ở runtime handle được (phụ thuộc vào setup của file efkproj trên công cụ Effekseer)
            // Tuy nhiên, nếu file efkproj đã tự loop, ta quản lý thêm thời gian tự hủy trên C# (nếu config cài đặt Duration).

            int id = _nextHandleID++;
            _activeHandleIDs.Add(id);
            _handleMap.Add(id, handle);

            if (configData.Duration > 0f)
            {
                _customDurations.Add(id, configData.Duration);
            }

            return id;
        }

        public void UpdateEffectPosition(int handleID, Vector3 newPosition)
        {
            if (_handleMap.TryGetValue(handleID, out EffekseerHandle handle) && handle.exists)
            {
                handle.SetLocation(newPosition);
            }
        }

        public void StopEffect(int handleID)
        {
            if (_handleMap.TryGetValue(handleID, out EffekseerHandle handle))
            {
                if (handle.exists)
                {
                    handle.Stop();
                }

                // Cleanup maps instantly
                _handleMap.Remove(handleID);
                _customDurations.Remove(handleID);
                RemoveFromNativeList(handleID);
            }
        }

        public void Tick()
        {
            float dt = Time.deltaTime;

            // 1. Cập nhật các timer nội bộ (nếu có vòng đời thủ công trên C#)
            // Tạm thời có thể cho thẳng vào vòng lặp này vì số lượng chạy custom duration thường ít

            // TODO: Ở scale lớn với hàng ngàn VFX, ta có thể tạo Job đếm lùi duration đồng bộ trước ở đây sử dụng NativeArray<float>

            // 2. Chạy dọn dẹp từ trên Main Thread vòng đời Effekseer
            for (int i = _activeHandleIDs.Length - 1; i >= 0; i--)
            {
                int id = _activeHandleIDs[i];

                // Bước đếm timer custom (nếu chạy quá thời gian => force stop)
                if (_customDurations.TryGetValue(id, out float timeLeft))
                {
                    timeLeft -= dt;
                    if (timeLeft <= 0f)
                    {
                        if (_handleMap.TryGetValue(id, out var h) && h.exists) h.Stop();
                        _customDurations.Remove(id);
                        _handleMap.Remove(id);
                        _activeHandleIDs.RemoveAtSwapBack(i);
                        continue;
                    }
                    _customDurations[id] = timeLeft;
                }

                // Check vòng đời gốc từ Effekseer
                if (_handleMap.TryGetValue(id, out EffekseerHandle handle))
                {
                    if (!handle.exists)
                    {
                        _handleMap.Remove(id);
                        _customDurations.Remove(id); // Cho chắc chắn
                        _activeHandleIDs.RemoveAtSwapBack(i);
                    }
                }
            }
        }

        private void RemoveFromNativeList(int idToRemove)
        {
            // O(N) tìm kiếm để swap back xóa O(1)
            // Nên gọi khi Stop thủ công. Hàm Tick sẽ tự RemoveAtSwapBack rất nhanh
            for (int i = 0; i < _activeHandleIDs.Length; i++)
            {
                if (_activeHandleIDs[i] == idToRemove)
                {
                    _activeHandleIDs.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        public void Dispose()
        {
            // Force stop all active effects when the manager is disposed
            foreach (var kvp in _handleMap)
            {
                if (kvp.Value.exists) kvp.Value.StopRoot();
            }

            _handleMap.Clear();
            _customDurations.Clear();
            if (_activeHandleIDs.IsCreated) _activeHandleIDs.Dispose();
        }
    }
}
