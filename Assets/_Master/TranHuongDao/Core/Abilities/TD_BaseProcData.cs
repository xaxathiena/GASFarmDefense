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

    public enum EProcTargetType
    {
        Enemy,
        Ally,
        Both
    }

    public enum EProcTargetSelection
    {
        HitTarget,
        Source,
        ClosestInAttackRange,
        RandomInAttackRange,
        AllInAttackRange
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

        [Header("Targeting Overhaul")]
        [Tooltip("How to select targets when the proc fires.")]
        public EProcTargetSelection targetSelection = EProcTargetSelection.HitTarget;
        
        [Tooltip("What kind of units can be targeted by this proc.")]
        public EProcTargetType targetType = EProcTargetType.Enemy;

        [Tooltip("Maximum number of targets to select (used for Random or Closest).")]
        public int targetCount = 1;

        [Header("Modular Proc Actions")]

        [Tooltip("Direct flat damage to deal upon proc (bypasses GameplayEffect complexity for simple nukes/strikes).")]
        public float flatDamage = 0f;

        [Tooltip("Prefab to instantiate upon proc (e.g. Goblin, Nuke VFX, Fireball).")]
        public GameObject prefabToSpawn;
    }
}
