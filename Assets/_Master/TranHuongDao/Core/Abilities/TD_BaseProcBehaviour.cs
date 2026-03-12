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
        private readonly GameplayAbilityLogic _logic;
        private readonly Dictionary<string, int> _attackCounts = new Dictionary<string, int>();

        [Inject]
        public TD_BaseProcBehaviour(IEnemyManager enemyManager, GameplayAbilityLogic logic)
        {
            _enemyManager = enemyManager;
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
            if (targetASC == null) return; // Need a target to proceed
            
            Vector3 centerPos = targetASC.GetOwner().position;
            
            // 1. AoE Handling vs Single Target
            if (procData.aoeRadius > 0f)
            {
                List<int> enemiesInRadius = new List<int>();
                _enemyManager.GetEnemiesInRange(centerPos, procData.aoeRadius, enemiesInRadius);
                
                foreach (var enemyID in enemiesInRadius)
                {
                    if (_enemyManager.TryGetEnemyASC(enemyID, out var aoeTargetASC))
                    {
                        ApplyProcActions(procData, sourceASC, aoeTargetASC);
                    }
                }
            }
            else
            {
                // Single-target execution
                if (procData.executionTarget == EProcContextTarget.Target)
                    ApplyProcActions(procData, sourceASC, targetASC);
                else
                    ApplyProcActions(procData, sourceASC, sourceASC); // Self
            }

            // 2. Trigger secondary ability recursively if configured
            if (procData.abilityToTrigger != null)
            {
                sourceASC.TryActivateAbility(procData.abilityToTrigger);
            }
        }

        private void ApplyProcActions(TD_BaseProcData procData, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            // A. Apply Gameplay Effect (Buffs, Debuffs, DoTs, CC)
            if (procData.effectToApply != null)
            {
                _logic.ApplyEffectToTarget(procData.effectToApply, source, target, procData, null);
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
