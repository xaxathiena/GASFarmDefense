using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Runtime instance of an active gameplay effect
    /// </summary>
    public class ActiveGameplayEffect
    {
        public readonly GameplayEffectService gameplayEffectService;
        public GameplayEffect Effect { get; private set; }
        public AbilitySystemComponent Source { get; private set; }
        public AbilitySystemComponent Target { get; private set; }
        public GameplayEffectContext Context { get; private set; }
        public float StartTime { get; private set; }
        public float Duration { get; private set; }
        public int StackCount { get; private set; }
        public float Level { get; private set; } = 1f;

        // Track affected attributes for cleanup
        private readonly List<GameplayAttribute> affectedAttributes = new();

        // Track individual stack expiration times (if policy is IndividualStackDuration)
        private readonly List<float> _stackExpirationTimes = new();

        // For periodic effects
        private float periodicTimer;
        private readonly bool isPeriodic;
        private readonly float period;

        public bool IsExpired
        {
            get
            {
                if (Effect.durationType == EGameplayEffectDurationType.Infinite) return false;
                if (Effect.durationType == EGameplayEffectDurationType.Instant) return true;
                
                if (Effect.allowStacking && Effect.stackingDurationPolicy == EGameplayEffectStackingDurationPolicy.IndividualStackDuration)
                {
                    return StackCount <= 0;
                }
                
                return (Time.time - StartTime) >= Duration;
            }
        }

        public float RemainingTime
        {
            get
            {
                if (Effect.durationType == EGameplayEffectDurationType.Infinite) return -1f;
                if (Effect.durationType == EGameplayEffectDurationType.Instant) return 0f;

                if (Effect.allowStacking && Effect.stackingDurationPolicy == EGameplayEffectStackingDurationPolicy.IndividualStackDuration)
                {
                    if (_stackExpirationTimes.Count == 0) return 0f;
                    // Return time until NEXT stack expires
                    return Mathf.Max(0, _stackExpirationTimes[0] - Time.time);
                }

                return Mathf.Max(0, Duration - (Time.time - StartTime));
            }
        }

        public event Action<ActiveGameplayEffect> OnEffectExpired;
        public event Action<ActiveGameplayEffect> OnEffectRemoved;

        public ActiveGameplayEffect(GameplayEffect effect, AbilitySystemComponent source, AbilitySystemComponent target, float level, GameplayEffectService gameplayEffectService, GameplayEffectContext context)
        {
            this.gameplayEffectService = gameplayEffectService;
            Effect = effect;
            Source = source;
            Target = target;
            Context = context;
            StartTime = Time.time;
            StackCount = 1;
            Level = Mathf.Max(1f, level);

            // Set duration based on effect type
            switch (effect.durationType)
            {
                case EGameplayEffectDurationType.Instant:
                    Duration = 0f;
                    break;
                case EGameplayEffectDurationType.Duration:
                    Duration = effect.durationMagnitude;
                    break;
                case EGameplayEffectDurationType.Infinite:
                    Duration = -1f; // Infinite
                    break;
            }

            // Setup periodic
            isPeriodic = effect.isPeriodic;
            period = effect.period;

            periodicTimer = period;
            
            // Initialize expiration times if duration based
            if (Duration > 0)
            {
                _stackExpirationTimes.Add(StartTime + Duration);
            }
        }

        /// <summary>
        /// Update the active effect (for duration and periodic effects)
        /// </summary>
        public void Update(float deltaTime)
        {
            bool stackChanged = false;

            // Handle individual stack expiration
            if (Effect.allowStacking && Effect.stackingDurationPolicy == EGameplayEffectStackingDurationPolicy.IndividualStackDuration && Duration > 0)
            {
                while (_stackExpirationTimes.Count > 0 && Time.time >= _stackExpirationTimes[0])
                {
                    _stackExpirationTimes.RemoveAt(0);
                    StackCount--;
                    stackChanged = true;
                }

                if (stackChanged)
                {
                    gameplayEffectService.RefreshModifiers(this);
                    if (StackCount <= 0)
                    {
                        OnEffectExpired?.Invoke(this);
                        return;
                    }
                }
            }

            // Check if expired (for other policies)
            if (IsExpired)
            {
                OnEffectExpired?.Invoke(this);
                return;
            }

            // Handle periodic execution
            if (isPeriodic && Effect.durationType != EGameplayEffectDurationType.Instant)
            {
                periodicTimer -= deltaTime;

                if (periodicTimer <= 0f)
                {
                    ExecutePeriodic();
                    periodicTimer = period;
                }
            }
        }

        /// <summary>
        /// Execute periodic effect
        /// </summary>
        private void ExecutePeriodic()
        {
            if (Target?.AttributeSet == null)
            {
                return;
            }

            // Periodic effects are like instant effects - apply directly to BaseValue
            foreach (var modifier in Effect.modifiers)
            {
                //Effect.ApplyModifierWithAggregation(Target.AttributeSet, modifier, Source, Target, Level, StackCount, this, true);
                gameplayEffectService.ApplyModifierWithAggregation(Effect, modifier, Source, Target, Level, StackCount, this, true, Context);

            }
        }

        /// <summary>
        /// Add a stack to this effect
        /// </summary>
        public bool AddStack()
        {
            if (!Effect.allowStacking)
                return false;

            if (StackCount >= Effect.maxStacks)
                return false;

            StackCount++;

            // Handle duration based on policy
            if (Effect.durationType == EGameplayEffectDurationType.Duration)
            {
                switch (Effect.stackingDurationPolicy)
                {
                    case EGameplayEffectStackingDurationPolicy.RefreshEntireStack:
                        if (Effect.refreshDurationOnStack)
                        {
                            StartTime = Time.time;
                            if (_stackExpirationTimes.Count > 0) _stackExpirationTimes[0] = StartTime + Duration;
                        }
                        break;

                    case EGameplayEffectStackingDurationPolicy.IndividualStackDuration:
                        _stackExpirationTimes.Add(Time.time + Duration);
                        // Optional: keep it sorted if Duration could theoretically vary per stack application
                        // _stackExpirationTimes.Sort(); 
                        break;

                    case EGameplayEffectStackingDurationPolicy.FixedDurationEntireStack:
                        // Do nothing to the timers
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Remove a stack from this effect
        /// </summary>
        public bool RemoveStack()
        {
            StackCount--;

            if (StackCount <= 0)
            {
                OnEffectRemoved?.Invoke(this);
                return true; // Effect should be removed
            }

            return false;
        }

        /// <summary>
        /// Add an attribute that this effect modifies (for cleanup)
        /// </summary>
        public void AddAffectedAttribute(GameplayAttribute attribute)
        {
            if (attribute != null && !affectedAttributes.Contains(attribute))
            {
                affectedAttributes.Add(attribute);
            }
        }

        /// <summary>
        /// Get all attributes affected by this effect
        /// </summary>
        public List<GameplayAttribute> GetAffectedAttributes()
        {
            return new List<GameplayAttribute>(affectedAttributes);
        }

        /// <summary>
        /// Get effect info as string
        /// </summary>
        public override string ToString()
        {
            string info = $"{Effect.effectName}";

            if (Effect.allowStacking)
                info += $" x{StackCount}";

            if (Duration > 0)
                info += $" ({RemainingTime:F1}s)";

            return info;
        }
    }
}
