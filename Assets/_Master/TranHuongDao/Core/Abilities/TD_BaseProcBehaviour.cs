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
            
            // Execute the outcome based on constraints
            ProcessProc(procData, asc);
        }

        // Logic block executed when proc conditions are met through external game events
        private void ProcessProc(TD_BaseProcData procData, AbilitySystemComponent sourceASC)
        {
            Vector3 centerPos = sourceASC.GetOwner().position;
            
            // Handling Target resolution (Simplified to current tracked/latest enemy for AoE demonstration)
            // Ideally, the external trigger should pass target ASC via Payload or Context.
            
            // Get all in AoE or single closest
            if (procData.aoeRadius > 0f)
            {
                List<int> enemiesInRadius = new List<int>();
                _enemyManager.GetEnemiesInRange(centerPos, procData.aoeRadius, enemiesInRadius);
                
                foreach (var enemyID in enemiesInRadius)
                {
                    if (_enemyManager.TryGetEnemyASC(enemyID, out var targetASC))
                    {
                        ApplyEffect(procData, sourceASC, targetASC);
                    }
                }
            }
            else
            {
                // Single execution fallback 
                int closestID = _enemyManager.GetClosestEnemyInRange(centerPos, 10f);
                if (closestID != -1 && _enemyManager.TryGetEnemyASC(closestID, out var targetASC))
                {
                    if (procData.executionTarget == EProcContextTarget.Target)
                        ApplyEffect(procData, sourceASC, targetASC);
                    else
                        ApplyEffect(procData, sourceASC, sourceASC); // Self
                }
            }

            // Trigger secondary ability recursively if configured
            if (procData.abilityToTrigger != null)
            {
                sourceASC.TryActivateAbility(procData.abilityToTrigger);
            }
        }

        private void ApplyEffect(TD_BaseProcData procData, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (procData.effectToApply != null)
            {
                _logic.ApplyEffectToTarget(procData.effectToApply, source, target, procData, null);
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
                asc.TryActivateAbility(procData);
            }
        }
    }
}
