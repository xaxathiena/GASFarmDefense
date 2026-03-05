using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    public enum EProcTriggerCondition 
    { 
        OnAttackStart, 
        OnHit, 
        OnKill, 
        EveryNthAttack 
    }

    public enum EProcContextTarget 
    { 
        Source, 
        Target 
    }

    /// <summary>
    /// Master Class for probabilities and conditioned effects (RNG Procs).
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Abilities/Proc Ability Data", fileName = "TD_BaseProcData")]
    public class TD_BaseProcData : GameplayAbilityData
    {
        [Header("Trigger Conditions")]
        public EProcTriggerCondition triggerType = EProcTriggerCondition.OnHit;
        
        [Range(0f, 100f), Tooltip("Chance to activate (%).")]
        public float chance = 15f;
        
        [Tooltip("Counter threshold for EveryNthAttack mode.")]
        public int countThreshold = 3;

        [Header("Outcome")]
        [Tooltip("If activated, to whom does it apply? Source (Tower) or Target (Enemy)?")]
        public EProcContextTarget executionTarget = EProcContextTarget.Target;
        
        [Tooltip("Gameplay effect to apply immediately upon successful proc.")]
        public GameplayEffect effectToApply;
        
        [Tooltip("Optional sub-ability to trigger upon successful proc.")]
        public GameplayAbilityData abilityToTrigger;

        [Tooltip("If > 0, effect is applied dynamically in an AoE radius. If 0, only pure single target.")]
        public float aoeRadius = 0f;
    }
}
