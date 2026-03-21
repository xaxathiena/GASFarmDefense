using System.Collections.Generic;
using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Utility component dynamically attached to run Aura checking logic.
    /// Handled efficiently via EnemyManager without Physics.
    /// </summary>
    public class TD_AuraInstance : MonoBehaviour
    {
        private AbilitySystemComponent _ownerASC;
        private TD_AuraData _data;
        private IEnemyManager _enemyManager;
        
        private readonly HashSet<int> _currentTargets = new HashSet<int>();
        private readonly Dictionary<int, ActiveGameplayEffect> _appliedEffects = new Dictionary<int, ActiveGameplayEffect>();
        private readonly List<int> _buffer = new List<int>(32); // Pre-allocate
        
        private float _nextTickTime;

        public void Initialize(AbilitySystemComponent asc, TD_AuraData data, IEnemyManager enemyManager)
        {
            _ownerASC = asc;
            _data = data;
            _enemyManager = enemyManager;
            _nextTickTime = Time.time;
        }

        private void Update()
        {
            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + _data.tickInterval;
                ScanAura();
            }
        }

        private void ScanAura()
        {
            _buffer.Clear();
            _enemyManager.GetEnemiesInRange(transform.position, _data.radius, _buffer);
            
            // Using a temporary HashSet within the method could allocate, but for lists usually <= 30 elements it's negligible.
            // Using Contains directly on _buffer is very fast.
            
            // Find who left the aura
            List<int> toRemove = new List<int>();
            foreach (var targetID in _currentTargets)
            {
                if (!_buffer.Contains(targetID))
                {
                    RemoveAuraEffect(targetID);
                    toRemove.Add(targetID);
                }
            }
            
            foreach (var rm in toRemove) _currentTargets.Remove(rm);

            // Find who entered the aura
            foreach (var targetID in _buffer)
            {
                if (!_currentTargets.Contains(targetID))
                {
                    ApplyAuraEffect(targetID);
                    _currentTargets.Add(targetID);
                }
            }
        }

        private void ApplyAuraEffect(int enemyID)
        {
            if (_data.auraEffect == null) return;
            
            if (_enemyManager.TryGetEnemyASC(enemyID, out var targetASC))
            {
                var activeEffect = targetASC.ApplyGameplayEffectToSelf(_data.auraEffect, _ownerASC, 1f);
                if (activeEffect != null)
                {
                    _appliedEffects[enemyID] = activeEffect;
                }
            }
        }

        private void RemoveAuraEffect(int enemyID)
        {
            if (_appliedEffects.TryGetValue(enemyID, out var activeEffect))
            {
                if (_enemyManager.TryGetEnemyASC(enemyID, out var targetASC))
                {
                    targetASC.RemoveGameplayEffect(activeEffect);
                }
                _appliedEffects.Remove(enemyID);
            }
        }

        private void OnDisable()
        {
            // Ensure proper cleanup if the tower is destroyed/deactivated
            foreach (var kvp in _appliedEffects)
            {
                if (_enemyManager.TryGetEnemyASC(kvp.Key, out var targetASC))
                {
                    targetASC.RemoveGameplayEffect(kvp.Value);
                }
            }
            _appliedEffects.Clear();
            _currentTargets.Clear();
        }
    }
}
