using UnityEngine;
using _Master.Base.Ability;

namespace _Master.Sample
{
    /// <summary>
    /// Base state for character AI FSM
    /// </summary>
    public abstract class CharacterState
    {
        protected CharacterAI characterAI;
        protected AbilitySystemComponent asc;
        
        public CharacterState(CharacterAI ai)
        {
            characterAI = ai;
            asc = ai.GetComponent<AbilitySystemComponent>();
        }
        
        /// <summary>
        /// Called when entering this state
        /// </summary>
        public virtual void OnEnter()
        {
        }
        
        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        public virtual void OnUpdate()
        {
        }
        
        /// <summary>
        /// Called when exiting this state
        /// </summary>
        public virtual void OnExit()
        {
        }
    }
    
    #region Idle State
    
    public class IdleState : CharacterState
    {
        public IdleState(CharacterAI ai) : base(ai) { }
        
        public override void OnEnter()
        {
            Debug.Log($"{characterAI.name}: Entered Idle state");
        }
        
        public override void OnUpdate()
        {
            // Check if should search for target
            if (Time.time - characterAI.lastActionTime >= characterAI.actionCooldown)
            {
                characterAI.ChangeState(ECharacterState.SearchTarget);
            }
        }
    }
    
    #endregion
    
    #region Search Target State
    
    public class SearchTargetState : CharacterState
    {
        public SearchTargetState(CharacterAI ai) : base(ai) { }
        
        public override void OnEnter()
        {
            Debug.Log($"{characterAI.name}: Searching for target...");
        }
        
        public override void OnUpdate()
        {
            // Find nearest enemy
            Collider[] enemies = Physics.OverlapSphere(
                characterAI.transform.position, 
                characterAI.detectionRange, 
                characterAI.enemyLayers
            );
            
            if (enemies.Length > 0)
            {
                // Found enemies, decide action
                DecideAction();
            }
            else
            {
                // No enemies, back to idle
                characterAI.ChangeState(ECharacterState.Idle);
            }
        }
        
        private void DecideAction()
        {
            // Get current health percentage
            var health = asc.AttributeSet.GetAttribute(EGameplayAttributeType.Health);
            float healthPercent = health.GetPercentage();
            
            // Check if heal skill is available (not on cooldown)
            bool canUseHealSkill = !asc.IsAbilityOnCooldown(characterAI.healSkillAbility);
            
            // Decision logic:
            // 1. If health < 50% and heal skill available -> Use heal skill
            // 2. Otherwise -> Use normal attack
            
            if (healthPercent < 0.5f && canUseHealSkill)
            {
                characterAI.ChangeState(ECharacterState.UseSkill);
            }
            else
            {
                characterAI.ChangeState(ECharacterState.NormalAttack);
            }
        }
    }
    
    #endregion
    
    #region Normal Attack State
    
    public class NormalAttackState : CharacterState
    {
        public NormalAttackState(CharacterAI ai) : base(ai) { }
        
        public override void OnEnter()
        {
            Debug.Log($"{characterAI.name}: Using Normal Attack!");
            
            // Try to activate normal attack ability
            bool success = asc.TryActivateAbility(characterAI.normalAttackAbility);
            
            if (success)
            {
                characterAI.lastActionTime = Time.time;
            }
            
            // Return to idle
            characterAI.ChangeState(ECharacterState.Idle);
        }
    }
    
    #endregion
    
    #region Use Skill State
    
    public class UseSkillState : CharacterState
    {
        public UseSkillState(CharacterAI ai) : base(ai) { }
        
        public override void OnEnter()
        {
            Debug.Log($"{characterAI.name}: Using Heal Skill!");
            
            // Try to activate heal skill
            bool success = asc.TryActivateAbility(characterAI.healSkillAbility);
            
            if (success)
            {
                characterAI.lastActionTime = Time.time;
            }
            
            // Return to idle
            characterAI.ChangeState(ECharacterState.Idle);
        }
    }
    
    #endregion
    
    #region Dead State
    
    public class DeadState : CharacterState
    {
        public DeadState(CharacterAI ai) : base(ai) { }
        
        public override void OnEnter()
        {
            Debug.Log($"{characterAI.name}: Died!");
            
            // Disable character
            characterAI.enabled = false;
        }
    }
    
    #endregion
}
