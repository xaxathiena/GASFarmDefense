using Abel.GAS.Abilities;
using Abel.GAS.Effects;
using UnityEngine;

namespace Abel.GAS.Samples.OrbCombat
{
    public class ApplyEffectAbilityBehaviour : IAbilityBehaviour
    {
        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) => true;

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var orbData = data as OrbAbilityData;
            if (orbData != null && spec.TargetContext != null)
            {
                foreach (var effect in orbData.effectsToApply)
                {
                    // Correct signature: (effect, target, source, level, sourceAbility, magnitudes)
                    asc.ApplyGameplayEffectToTarget(effect, spec.TargetContext, asc, spec.Level, data);
                }
            }
            asc.EndAbility(data);
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    }
}
