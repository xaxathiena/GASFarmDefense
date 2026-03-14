using System.Collections.Generic;
using UnityEngine;
using VContainer;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Acts as evaluating component for conditional procs.
    /// Since it's decoupled from update loop, an External trigger will use tryActivate manually.
    /// Alternatively, Tower systems can listen to events and direct calls here.
    /// </summary>
    public class TD_BaseProcBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        private readonly ITowerManager _towerManager;
        private readonly GameplayAbilityLogic _logic;
        private readonly Dictionary<string, int> _attackCounts = new Dictionary<string, int>();

        [Inject]
        public TD_BaseProcBehaviour(IEnemyManager enemyManager, ITowerManager towerManager, GameplayAbilityLogic logic)
        {
            _enemyManager = enemyManager;
            _towerManager = towerManager;
            _logic = logic;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var procData = data as TD_BaseProcData;
            if (procData == null) return false;

            // Evaluated explicitly during external Triggers.
            // RNG checks shouldn't happen here otherwise CanActivate fails silently during pre-flight.
            return true;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var procData = data as TD_BaseProcData;
            if (procData == null) return;

            // Note: Since OnActivated doesn't pass the custom trigger target payload implicitly in basic GAS yet,
            // we bypass OnActivated for data-driven procs and directly evaluate them through EvaluateProc -> ProcessProc.
            // This is a common pattern for fast on-hit triggers.
        }

        // Logic block executed when proc conditions are met through external game events
        private void ProcessProc(TD_BaseProcData procData, AbilitySystemComponent sourceASC, AbilitySystemComponent targetASC)
        {
            List<AbilitySystemComponent> resolvedTargets = ResolveTargets(procData, sourceASC, targetASC);

            if (resolvedTargets.Count == 0) return;

            foreach (var target in resolvedTargets)
            {
                ApplyProcActions(procData, sourceASC, target);
            }

            // Trigger secondary ability recursively if configured
            if (procData.abilityToTrigger != null)
            {
                // We trigger the ability from the source once.
                // If the secondary ability needs target payloads, GAS would need that context pass-through,
                // but currently TryActivateAbility is self-contained.
                sourceASC.TryActivateAbility(procData.abilityToTrigger);
            }
        }

        private List<AbilitySystemComponent> ResolveTargets(TD_BaseProcData procData, AbilitySystemComponent sourceASC, AbilitySystemComponent targetASC)
        {
            var targets = new List<AbilitySystemComponent>();
            var sourceOwner = sourceASC.GetOwner();
            var ignoreList = new List<AbilitySystemComponent>() { sourceASC };
            if (sourceOwner == null) return targets;

            Vector3 centerPos = sourceOwner.position;
            var attrSet = sourceASC.GetAttributeSet<UnitAttributeSet>();
            float attackRange = attrSet != null ? attrSet.AttackRange.CurrentValue : 5f;

            switch (procData.targetSelection)
            {
                case EProcTargetSelection.Source:
                    if (MatchesTargetType(procData.targetType, true))
                        targets.Add(sourceASC);
                    break;

                case EProcTargetSelection.HitTarget:
                    if (targetASC != null && MatchesTargetType(procData.targetType, false))
                    {
                        targets.Add(targetASC);
                    }
                    else if (targetASC == null)
                    {
                        // Fallback: If no hit target provided (e.g. OnAttackStart), find the closest valid target
                        FindEnemiesInRange(centerPos, attackRange, procData.targetType, 1, ignoreList, targets);
                    }
                    break;

                case EProcTargetSelection.ClosestInAttackRange:
                    FindEnemiesInRange(centerPos, attackRange, procData.targetType, procData.targetCount, ignoreList, targets);
                    break;

                case EProcTargetSelection.RandomInAttackRange:
                    FindEnemiesInRange(centerPos, attackRange, procData.targetType, procData.targetCount, ignoreList, targets, true);
                    break;

                case EProcTargetSelection.AllInAttackRange:
                    FindEnemiesInRange(centerPos, attackRange, procData.targetType, int.MaxValue, ignoreList, targets);
                    break;
            }

            // Never apply to self unless explicitly targeting Source.
            if (procData.targetSelection != EProcTargetSelection.Source)
                targets.Remove(sourceASC);

            return targets;
        }

        private void FindEnemiesInRange(Vector3 centerPos, float radius, EProcTargetType targetType, int maxCount, List<AbilitySystemComponent> ignoreList, List<AbilitySystemComponent> results, bool random = false)
        {
            if (targetType == EProcTargetType.Ally)
            {
                // Ally targets → query towers in range via ITowerManager.
                var allyBuffer = new List<AbilitySystemComponent>();
                _towerManager.GetTowersInRange(centerPos, radius, ignoreList, allyBuffer, maxCount);

                if (random && allyBuffer.Count > 1)
                {
                    for (int i = 0; i < allyBuffer.Count; i++)
                    {
                        var temp = allyBuffer[i];
                        int randomIndex = Random.Range(i, allyBuffer.Count);
                        allyBuffer[i] = allyBuffer[randomIndex];
                        allyBuffer[randomIndex] = temp;
                    }
                }

                int allyCount = 0;
                foreach (var allyASC in allyBuffer)
                {
                    if (allyCount >= maxCount) break;
                    results.Add(allyASC);
                    allyCount++;
                }
                return;
            }

            // Enemy / Both targets → query enemies in range via IEnemyManager.
            List<int> enemiesInRadius = new List<int>();
            _enemyManager.GetEnemiesInRange(centerPos, radius, enemiesInRadius);

            if (enemiesInRadius.Count == 0) return;

            // Simple random shuffle if requested
            if (random && enemiesInRadius.Count > 1)
            {
                for (int i = 0; i < enemiesInRadius.Count; i++)
                {
                    int temp = enemiesInRadius[i];
                    int randomIndex = Random.Range(i, enemiesInRadius.Count);
                    enemiesInRadius[i] = enemiesInRadius[randomIndex];
                    enemiesInRadius[randomIndex] = temp;
                }
            }

            int count = 0;
            foreach (var enemyID in enemiesInRadius)
            {
                if (count >= maxCount) break;

                if (_enemyManager.TryGetEnemyASC(enemyID, out var asc))
                {
                    results.Add(asc);
                    count++;
                }
            }
        }

        private bool MatchesTargetType(EProcTargetType type, bool isAlly)
        {
            if (type == EProcTargetType.Both) return true;
            if (type == EProcTargetType.Ally && isAlly) return true;
            if (type == EProcTargetType.Enemy && !isAlly) return true;
            return false;
        }

        private void ApplyProcActions(TD_BaseProcData procData, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            // A. Apply Gameplay Effect (Buffs, Debuffs, DoTs, CC)
            // Uses source.ApplyGameplayEffectToTarget so the full AbilitySystemLogic pipeline runs,
            // including CanApplyTo check and allowStacking / maxStacks enforcement.
            if (procData.effectToApply != null)
            {
                source.ApplyGameplayEffectToTarget(procData.effectToApply, target, source);
            }

            // B. Instantly apply Flat Damage directly to the UnitAttributeSet
            if (procData.flatDamage > 0f)
            {
                var targetAttrSet = target.GetAttributeSet<UnitAttributeSet>();
                if (targetAttrSet != null)
                {
                    targetAttrSet.TakeDamage(procData.flatDamage);
                }
            }

            // C. Instantiate external Prefab (e.g. Goblin, Nuke VFX)
            if (procData.prefabToSpawn != null && target.GetOwner() != null)
            {
                // For spawning, we instantiate exactly at the target's position.
                // The prefab should have its own logic for cleanup or subsequent actions.
                Object.Instantiate(procData.prefabToSpawn, target.GetOwner().position, Quaternion.identity);
            }
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }

        /// <summary>
        /// External hook for tower/game systems to submit proc evaluations.
        /// Normally an Event System relays this.
        /// </summary>
        public void EvaluateProc(GameplayAbilityData data, AbilitySystemComponent asc, EProcTriggerCondition triggerCtx, AbilitySystemComponent targetAsc = null)
        {
            var procData = data as TD_BaseProcData;
            if (procData == null || procData.triggerType != triggerCtx) return;

            bool isTriggered = false;

            if (procData.triggerType == EProcTriggerCondition.EveryNthAttack)
            {
                _attackCounts.TryAdd(asc.Id, 0);
                _attackCounts[asc.Id]++;

                if (_attackCounts[asc.Id] >= procData.countThreshold)
                {
                    isTriggered = true;
                    _attackCounts[asc.Id] = 0;
                }
            }
            else
            {
                if (Random.Range(0f, 100f) <= procData.chance)
                    isTriggered = true;
            }

            if (isTriggered)
            {
                // Instead of TryActivateAbility (which loses the `targetAsc` payload in base GAS),
                // we directly process the action with the valid target payload here.
                ProcessProc(procData, asc, targetAsc);
            }
        }
    }
}
