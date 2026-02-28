using System.Collections.Generic;
using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Stateless IAbilityBehaviour for <see cref="TDTowerSkillData"/>.
    /// Registered as a Singleton in VContainer; one instance is shared across all towers.
    ///
    /// Activation flow:
    ///   CanActivate  → at least one enemy within range and a skillEffect is assigned
    ///   OnActivated  → apply skillEffect to all targets (AoE or single), start cooldown
    /// </summary>
    public class TDTowerSkillBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;

        // Pre-allocated buffer shared by all activations — safe because GAS runs single-threaded.
        private readonly List<int> _targetBuffer = new List<int>(16);

        public TDTowerSkillBehaviour(IEnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        // ── IAbilityBehaviour ────────────────────────────────────────────────────

        public bool CanActivate(
            GameplayAbilityData data,
            AbilitySystemComponent asc,
            GameplayAbilitySpec spec)
        {
            if (data is not TDTowerSkillData skillData) return false;
            if (skillData.skillEffect == null) return false;

            var unitAttr = asc.GetAttributeSet<UnitAttributeSet>();
            if (unitAttr == null) return false;

            // Require at least one enemy in attack range before spending the cooldown.
            Vector3 origin = GetOwnerPosition(asc);
            return _enemyManager.GetClosestEnemyInRange(origin, unitAttr.AttackRange.CurrentValue) != -1;
        }

        public void OnActivated(
            GameplayAbilityData data,
            AbilitySystemComponent asc,
            GameplayAbilitySpec spec)
        {
            var skillData = data as TDTowerSkillData;
            if (skillData == null || skillData.skillEffect == null) return;

            var unitAttr = asc.GetAttributeSet<UnitAttributeSet>();
            if (unitAttr == null) return;

            Vector3 origin = GetOwnerPosition(asc);

            if (skillData.aoeRadius > 0f)
            {
                // AoE mode — collect all enemies inside the explicit radius.
                _targetBuffer.Clear();
                _enemyManager.GetEnemiesInRange(origin, skillData.aoeRadius, _targetBuffer);

                foreach (int targetID in _targetBuffer)
                    ApplySkillToEnemy(targetID, skillData, asc);

                Debug.Log($"[TowerSkill] AoE hit {_targetBuffer.Count} enemies (radius={skillData.aoeRadius})");
            }
            else
            {
                // Single-target mode — closest enemy within attack range.
                int targetID = _enemyManager.GetClosestEnemyInRange(origin, unitAttr.AttackRange.CurrentValue);
                if (targetID != -1)
                {
                    ApplySkillToEnemy(targetID, skillData, asc);
                    Debug.Log($"[TowerSkill] Single-target skill applied to enemy {targetID}");
                }
            }
        }

        public void OnEnded    (GameplayAbilityData _, AbilitySystemComponent __, GameplayAbilitySpec ___) { }
        public void OnCancelled(GameplayAbilityData _, AbilitySystemComponent __, GameplayAbilitySpec ___) { }

        // ── Private ──────────────────────────────────────────────────────────────

        private void ApplySkillToEnemy(
            int enemyID,
            TDTowerSkillData skillData,
            AbilitySystemComponent sourceASC)
        {
            if (!_enemyManager.TryGetEnemyASC(enemyID, out var targetASC)) return;

            // Apply the GameplayEffect directly from the source (tower) to the target (enemy).
            sourceASC.ApplyGameplayEffectToTarget(skillData.skillEffect, targetASC, sourceASC);
        }

        private static Vector3 GetOwnerPosition(AbilitySystemComponent asc)
        {
            var owner = asc.GetOwner();
            return owner != null ? owner.position : Vector3.zero;
        }
    }
}
