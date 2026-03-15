using System.Collections.Generic;
using UnityEngine;
using GAS;
using FD.Modules.VFX;

namespace Abel.TranHuongDao.Core.Abilities
{
    public class TD_InstantAoEAbilityBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        private readonly ITowerManager _towerManager;
        private readonly IVFXManager _vfxManager;

        // Reusable caches to minimize GC allocations
        private readonly List<AbilitySystemComponent> _targetsCache = new List<AbilitySystemComponent>(32);
        private readonly List<int> _idCache = new List<int>(32);
        private readonly List<AbilitySystemComponent> _emptyIgnoreList = new List<AbilitySystemComponent>();

        public TD_InstantAoEAbilityBehaviour(
            IEnemyManager enemyManager,
            ITowerManager towerManager,
            IVFXManager vfxManager)
        {
            _enemyManager = enemyManager;
            _towerManager = towerManager;
            _vfxManager = vfxManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            return data is TD_InstantAoEAbilityData;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var aoeData = data as TD_InstantAoEAbilityData;
            if (aoeData == null) return;

            // ── Resolve Origin ────────────────────────────────────────────────────
            // Use TargetContext if provided (e.g. hit location from proc), fallback to caster.
            Vector3 originPos = (spec.TargetContext != null && spec.TargetContext.Avatar != null)
                ? spec.TargetContext.Position
                : asc.Position;

            // ── Visuals ───────────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(aoeData.explosionVfxID))
            {
                _vfxManager.PlayEffectAt(aoeData.explosionVfxID, originPos);
            }

            // ── Query Targets ─────────────────────────────────────────────────────
            _targetsCache.Clear();
            _idCache.Clear();

            if (aoeData.targetType == EAuraTargetType.Enemies || aoeData.targetType == EAuraTargetType.Both)
            {
                _enemyManager.GetEnemiesInRange(originPos, aoeData.radius, _idCache);
                foreach (var id in _idCache)
                {
                    if (_enemyManager.TryGetEnemyASC(id, out var targetASC))
                    {
                        _targetsCache.Add(targetASC);
                    }
                }
            }

            if (aoeData.targetType == EAuraTargetType.Allies || aoeData.targetType == EAuraTargetType.Both)
            {
                // GetTowersInRange fills the results list directly with ASCs
                _towerManager.GetTowersInRange(originPos, aoeData.radius, _emptyIgnoreList, _targetsCache);
            }

            // ── Apply Effects ─────────────────────────────────────────────────────
            int count = 0;
            Dictionary<string, float> damagePayload = null;

            if (aoeData.damageEffect != null && !string.IsNullOrEmpty(aoeData.damageSetByCallerTag))
            {
                damagePayload = new Dictionary<string, float>
                {
                    { aoeData.damageSetByCallerTag, aoeData.damageAmount }
                };
            }

            foreach (var target in _targetsCache)
            {
                // Respect max targets limit
                if (aoeData.maxTargets != -1 && count >= aoeData.maxTargets) break;

                // Apply Damage via GE + SetByCaller
                if (aoeData.damageEffect != null)
                {
                    asc.ApplyGameplayEffectToTarget(aoeData.damageEffect, target, asc, 1f, aoeData, damagePayload);
                }

                // Apply Status Effect
                if (aoeData.statusEffect != null)
                {
                    asc.ApplyGameplayEffectToTarget(aoeData.statusEffect, target, asc, 1f, aoeData);
                }

                count++;
            }

            // Instant abilities typically end immediately
            asc.EndAbility(data);
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    }
}
