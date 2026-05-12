using System;
using System.Collections.Generic;
using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
using FD;
using FD.Modules.VFX;
using R3;
using UnityEngine;

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

        private readonly AbilitySystemComponent _asc;
        private readonly Dictionary<GameplayTag, int> _activeVFXHandles = new();

        public StatusEffectVFXController(int targetInstanceID, Func<Vector3> getPosition, AbilitySystemComponent asc, IVFXManager vfxManager, TagVFXConfig vfxConfig)
        {
            _targetInstanceID = targetInstanceID;
            _getPosition = getPosition;
            _asc = asc;
            _vfxManager = vfxManager;
            _vfxConfig = vfxConfig;

            if (_vfxConfig == null || _asc == null || _vfxManager == null) return;

            // Subscribe to the ASC events directly
            _asc.OnTagChanged += OnGameplayTagChanged;
        }

        private void OnGameplayTagChanged(GameplayTag tag, int newCount)
        {
            var vfxData = _vfxConfig.GetVFXData(tag);
            if (vfxData == null) return;

            if (newCount > 0)
            {
                // Tag added or increased. Play if not already playing.
                if (!_activeVFXHandles.ContainsKey(tag))
                {
                    var handleID = _vfxManager.PlayEffectAt(vfxData.vfxID, GetCurrentPositionWithOffset(vfxData.offset));
                    if (handleID >= 0) // Valid handle?
                    {
                        _activeVFXHandles[tag] = handleID;
                    }
                }
            }
            else
            {
                // Tag completely removed.
                if (_activeVFXHandles.TryGetValue(tag, out var handleID))
                {
                    _vfxManager.StopEffect(handleID);
                    _activeVFXHandles.Remove(tag);
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

            if (_asc != null)
            {
                _asc.OnTagChanged -= OnGameplayTagChanged;
            }

            // Unsubscribe from other things
            _disposables.Dispose();
        }
    }
}


