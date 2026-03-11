using System;
using System.Collections.Generic;
using UnityEngine;
using R3;
using FD;
using GAS;
using FD.Modules.VFX;

namespace Abel.TranHuongDao.Core.VFX
{
    /// <summary>
    /// Listens to GameplayTagChangedEvent published by GAS via the EventBus,
    /// and manages the spawning, tracking, and stopping of VFX for status effects.
    /// </summary>
    public class StatusEffectVFXController : IDisposable
    {
        private readonly IVFXManager _vfxManager;
        private readonly TagVFXConfig _vfxConfig;
        
        private readonly int _targetInstanceID;
        private readonly Func<Vector3> _getPosition;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        // Track active VFX handles for each tag.
        private readonly Dictionary<GameplayTag, int> _activeVFXHandles = new Dictionary<GameplayTag, int>();

        public StatusEffectVFXController(int targetInstanceID, Func<Vector3> getPosition, IEventBus eventBus, IVFXManager vfxManager, TagVFXConfig vfxConfig)
        {
            _targetInstanceID = targetInstanceID;
            _getPosition = getPosition;
            _vfxManager = vfxManager;
            _vfxConfig = vfxConfig;

            if (_vfxConfig == null || eventBus == null || _vfxManager == null) return;

            // Subscribe to the global GameplayTagChangedEvent specifically for our target's Instance ID.
            eventBus.Receive<GameplayTagChangedEvent>()
                .Where(evt => evt.OwnerInstanceID == _targetInstanceID)
                .Subscribe(OnGameplayTagChanged)
                .AddTo(_disposables);
        }

        private void OnGameplayTagChanged(GameplayTagChangedEvent evt)
        {
            var vfxData = _vfxConfig.GetVFXData(evt.Tag);
            if (vfxData == null) return;

            if (evt.NewCount > 0)
            {
                // Tag added or increased. Play if not already playing.
                if (!_activeVFXHandles.ContainsKey(evt.Tag))
                {
                    var handleID = _vfxManager.PlayEffectAt(vfxData.vfxID, GetCurrentPositionWithOffset(vfxData.offset));
                    if (handleID >= 0) // Valid handle?
                    {
                        _activeVFXHandles[evt.Tag] = handleID;
                    }
                }
            }
            else
            {
                // Tag completely removed.
                if (_activeVFXHandles.TryGetValue(evt.Tag, out var handleID))
                {
                    _vfxManager.StopEffect(handleID);
                    _activeVFXHandles.Remove(evt.Tag);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            // Update positions of all active VFX to follow the character.
            foreach (var kvp in _activeVFXHandles)
            {
                var tag = kvp.Key;
                var handleID = kvp.Value;
                var vfxData = _vfxConfig.GetVFXData(tag);
                if (vfxData != null)
                {
                    _vfxManager.UpdateEffectPosition(handleID, GetCurrentPositionWithOffset(vfxData.offset));
                }
            }
        }

        private Vector3 GetCurrentPositionWithOffset(Vector3 offset)
        {
            if (_getPosition == null) return offset;
            return _getPosition() + offset;
        }

        public void Dispose()
        {
            // Stop all playing VFX
            foreach (var handleID in _activeVFXHandles.Values)
            {
                _vfxManager.StopEffect(handleID);
            }
            _activeVFXHandles.Clear();

            // Unsubscribe from EventBus
            _disposables.Dispose();
        }
    }
}
