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
        public float StartTime { get; private set; }
        public float Duration { get; private set; }
        public int StackCount { get; private set; }
        public float Level { get; private set; } = 1f;

        // Track affected attributes for cleanup
        private List<GameplayAttribute> affectedAttributes = new List<GameplayAttribute>();

        // For periodic effects
        private float periodicTimer;
        private bool isPeriodic;
        private float period;

        public bool IsExpired => Duration > 0 && (Time.time - StartTime) >= Duration;
        public float RemainingTime => Duration > 0 ? Mathf.Max(0, Duration - (Time.time - StartTime)) : -1f;

        public event Action<ActiveGameplayEffect> OnEffectExpired;
        public event Action<ActiveGameplayEffect> OnEffectRemoved;

        public ActiveGameplayEffect(GameplayEffect effect, AbilitySystemComponent source, AbilitySystemComponent target, float level, GameplayEffectService gameplayEffectService)
        {
            this.gameplayEffectService = gameplayEffectService;
            Effect = effect;
            Source = source;
            Target = target;
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
        }

        /// <summary>
        /// Update the active effect (for duration and periodic effects)
        /// </summary>
        public void Update(float deltaTime)
        {
            // Check if expired
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
                gameplayEffectService.ApplyModifierWithAggregation(Effect, modifier, Source, Target, Level, StackCount, this, true, null);

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

            // Refresh duration if needed
            if (Effect.refreshDurationOnStack && Effect.durationType == EGameplayEffectDurationType.Duration)
            {
                StartTime = Time.time;
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
