using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Effects;
using Abel.GAS.Cues;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Abel.GAS.Samples.OrbCombat
{
    public class OrbCombatLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 1. Register Core GAS Services
            builder.Register<AbilitySystemLogic>(Lifetime.Singleton);
            builder.Register<GameplayAbilityLogic>(Lifetime.Singleton);
            builder.Register<GameplayEffectService>(Lifetime.Singleton);
            builder.Register<GameplayEffectCalculationService>(Lifetime.Singleton);
            builder.Register<AbilityBehaviourRegistry>(Lifetime.Singleton);
            
            // Register Cue System
            builder.RegisterComponentInHierarchy<GameplayCueManager>().AsImplementedInterfaces().AsSelf();

            // 2. Register Logger

            builder.Register<IGASLogger, UnityGASLogger>(Lifetime.Singleton);

            // 3. Register AbilitySystemComponent as Transient (each unit gets its own instance)
            builder.Register<AbilitySystemComponent>(Lifetime.Transient);

            // 4. Register Sample Game Logic

            builder.RegisterEntryPoint<OrbGameInitializer>();
        }
    }

    /// <summary>
    /// Simple logger for the sample.
    /// </summary>
    public class UnityGASLogger : IGASLogger
    {
        public void Log(string message, Color color = default)
        {
            if (color == default) color = Color.white;
            Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>[GAS]</color> {message}");
        }

        public void LogWarning(string message) => Debug.LogWarning($"[GAS] {message}");
        public void LogError(string message) => Debug.LogError($"[GAS] {message}");
    }
}
