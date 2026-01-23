using System.Collections.Generic;
using UnityEngine;
using _Master.Base.Ability;

namespace _Master.Sample
{
    /// <summary>
    /// Character AI controller using FSM (Finite State Machine)
    /// </summary>
    [RequireComponent(typeof(AbilitySystemComponent))]
    public class CharacterAI : MonoBehaviour
    {
        [Header("Abilities")]
        [Tooltip("Normal attack ability")]
        public GameplayAbility normalAttackAbility;
        
        [Tooltip("Heal skill ability (10s cooldown)")]
        public GameplayAbility healSkillAbility;
        
        [Header("AI Settings")]
        [Tooltip("Detection range for enemies")]
        public float detectionRange = 10f;
        
        [Tooltip("Time between actions")]
        public float actionCooldown = 1f;
        
        [Tooltip("Enemy layers")]
        public LayerMask enemyLayers;
        
        // FSM
        private Dictionary<ECharacterState, CharacterState> states = new Dictionary<ECharacterState, CharacterState>();
        private CharacterState currentState;
        private ECharacterState currentStateType;
        
        // Components
        private AbilitySystemComponent asc;
        
        // Public for states to access
        [HideInInspector] public float lastActionTime;
        
        private void Awake()
        {
            asc = GetComponent<AbilitySystemComponent>();
            
            // Initialize FSM states
            states[ECharacterState.Idle] = new IdleState(this);
            states[ECharacterState.SearchTarget] = new SearchTargetState(this);
            states[ECharacterState.NormalAttack] = new NormalAttackState(this);
            states[ECharacterState.UseSkill] = new UseSkillState(this);
            states[ECharacterState.Dead] = new DeadState(this);
            
            // Set initial state
            currentStateType = ECharacterState.Idle;
            currentState = states[currentStateType];
        }
        
        private void Start()
        {
            // Grant abilities
            if (normalAttackAbility != null)
            {
                asc.GiveAbility(normalAttackAbility);
            }
            
            if (healSkillAbility != null)
            {
                asc.GiveAbility(healSkillAbility);
            }
            
            // Subscribe to health changes
            var attributes = asc.AttributeSet;
            if (attributes != null)
            {
                var health = attributes.GetAttribute(EGameplayAttributeType.Health);
                if (health != null)
                {
                    health.OnValueChanged += OnHealthChanged;
                }
            }
            
            // Enter initial state
            currentState.OnEnter();
            
            Debug.Log($"{name}: Character AI initialized!");
        }
        
        private void Update()
        {
            if (currentState != null)
            {
                currentState.OnUpdate();
            }
        }
        
        /// <summary>
        /// Change to a new state
        /// </summary>
        public void ChangeState(ECharacterState newStateType)
        {
            if (currentStateType == newStateType)
                return;
            
            // Exit current state
            currentState?.OnExit();
            
            // Change state
            currentStateType = newStateType;
            currentState = states[newStateType];
            
            // Enter new state
            currentState?.OnEnter();
        }
        
        /// <summary>
        /// Get current state
        /// </summary>
        public ECharacterState GetCurrentState()
        {
            return currentStateType;
        }
        
        /// <summary>
        /// Called when health changes
        /// </summary>
        private void OnHealthChanged(float oldValue, float newValue)
        {
            Debug.Log($"{name}: Health changed {oldValue} -> {newValue}");
            
            // Check if dead
            if (newValue <= 0 && currentStateType != ECharacterState.Dead)
            {
                ChangeState(ECharacterState.Dead);
            }
        }
        
        /// <summary>
        /// Debug visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Draw attack range (if normal attack ability exists)
            if (normalAttackAbility != null && normalAttackAbility is NormalAttackAbility normalAttack)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, normalAttack.attackRange);
            }
        }
    }
}
