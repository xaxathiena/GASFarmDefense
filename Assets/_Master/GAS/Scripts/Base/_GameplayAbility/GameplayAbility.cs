using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Legacy GameplayAbility class - now acts as an adapter to the new Data + Behaviour architecture.
    /// For backward compatibility with existing abilities.
    /// New abilities should inherit from GameplayAbilityData + implement IAbilityBehaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameplayAbility", menuName = "GAS/Base/Gameplay Ability (Legacy)")]
    public class GameplayAbility : GameplayAbilityData
    {
        // Static logic accessor (set by VContainer initialization)
        private static GameplayAbilityLogic _logic;
        private static AbilityBehaviourRegistry _registry;
        
        public static void SetLogic(GameplayAbilityLogic logic) => _logic = logic;
        public static void SetRegistry(AbilityBehaviourRegistry registry) => _registry = registry;
        
        // Public accessors for logic
        protected GameplayAbilityLogic Logic => _logic;
        public static GameplayAbilityLogic GetLogic() => _logic;
        
        // Legacy properties are now in GameplayAbilityData base class
        
        /// <summary>
        /// Override to return custom behaviour type.
        /// Default returns LegacyAbilityBehaviour which uses the old virtual method pattern.
        /// </summary>
        public override System.Type GetBehaviourType()
        {
            return typeof(LegacyAbilityBehaviour);
        }

        /// <summary>
        /// Check if the ability can be activated using its resolved spec.
        /// </summary>
        public bool CanActivateAbility(AbilitySystemComponent asc)
        {
            var spec = asc?.GetAbilitySpec(this);
            return CanActivateAbility(asc, spec);
        }

        public virtual bool CanActivateAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (_registry == null)
            {
                // Fallback to old logic if registry not initialized
                return Logic.CanActivateAbility(this, asc, spec);
            }
            
            var behaviour = _registry.GetBehaviour(this);
            if (behaviour != null)
            {
                return behaviour.CanActivate(this, asc, spec);
            }
            
            return Logic.CanActivateAbility(this, asc, spec);
        }

        /// <summary>
        /// Activate the ability.
        /// </summary>
        public void ActivateAbility(AbilitySystemComponent asc)
        {
            var spec = asc?.GetAbilitySpec(this);
            ActivateAbility(asc, spec);
        }

        public virtual void ActivateAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Delegate to logic for base activation (cost, cooldown, tags)
            Logic.ActivateAbility(this, asc, spec);
            
            // Call behaviour or legacy hook
            if (_registry != null)
            {
                var behaviour = _registry.GetBehaviour(this);
                if (behaviour != null)
                {
                    behaviour.OnActivated(this, asc, spec);
                    return;
                }
            }
            
            // Fallback to legacy virtual method
            OnAbilityActivated(asc, spec);
        }

        /// <summary>
        /// Override this to implement ability-specific logic.
        /// </summary>
        protected virtual void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
        }

        public void EndAbility(AbilitySystemComponent asc)
        {
            var spec = asc?.GetAbilitySpec(this);
            EndAbility(asc, spec);
        }

        public virtual void EndAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Delegate to logic for base cleanup
            Logic.EndAbility(this, asc, spec);
            
            // Call behaviour or legacy hook
            if (_registry != null)
            {
                var behaviour = _registry.GetBehaviour(this);
                if (behaviour != null)
                {
                    behaviour.OnEnded(this, asc, spec);
                    return;
                }
            }
            
            // Fallback to legacy virtual method
            OnAbilityEnded(asc, spec);
        }

        protected virtual void OnAbilityEnded(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
        }

        public virtual void CancelAbility(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (asc == null || spec == null || !spec.IsActive)
                return;

            // Call behaviour or legacy hook
            if (_registry != null)
            {
                var behaviour = _registry.GetBehaviour(this);
                if (behaviour != null)
                {
                    behaviour.OnCancelled(this, asc, spec);
                    Logic.CancelAbility(this, asc, spec);
                    return;
                }
            }
            
            // Fallback to legacy virtual method
            OnAbilityCancelled(asc, spec);
            Logic.CancelAbility(this, asc, spec);
        }

        protected virtual void OnAbilityCancelled(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
        }

        protected float GetAbilityLevel(AbilitySystemComponent asc)
        {
            if (asc == null)
                return 1f;

            var spec = asc.GetAbilitySpec(this);
            return GetAbilityLevel(spec);
        }

        protected float GetAbilityLevel(GameplayAbilitySpec spec)
        {
            return Logic.GetAbilityLevel(spec);
        }

        /// <summary>
        /// Public helper for non-ability classes (e.g., projectiles) to apply effects using FD context.
        /// </summary>
        public void ApplyEffectToTarget(GameplayEffect effect, AbilitySystemComponent source, AbilitySystemComponent target, GameplayAbilitySpec spec)
        {
            Logic.ApplyEffectToTarget(effect, source, target, this, spec);
        }

        /// <summary>
        /// Apply effect with FD context
        /// </summary>
        protected void ApplyEffectWithContext(GameplayEffect effect, AbilitySystemComponent source, AbilitySystemComponent target, GameplayAbilitySpec spec)
        {
            Logic.ApplyEffectToTarget(effect, source, target, this, spec);
        }

        protected virtual GameplayEffectContext CreateFDContext(AbilitySystemComponent source, AbilitySystemComponent target, GameplayAbilitySpec spec)
        {
            return new GameplayEffectContext
            {
                SourceASC = source,
                TargetASC = target,
                SourceAbility = this,
                Level = GetAbilityLevel(spec)
            };
        }

        protected GameObject GetAbilityOwner(AbilitySystemComponent asc)
        {
            return Logic.GetAbilityOwner(asc);
        }
    }
}
