using Abel.GAS.Abilities;
using Abel.GAS.Effects;
using UnityEngine;

namespace Abel.GAS.Samples.OrbCombat
{
    public class FireballAbilityBehaviour : IAbilityBehaviour
    {
        private readonly GameplayEffectService _effectService;

        public FireballAbilityBehaviour(GameplayEffectService effectService)
        {
            _effectService = effectService;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var attributeSet = asc.AttributeSet as OrbAttributeSet;
            if (attributeSet != null && attributeSet.Mana.CurrentValue < 20f)
            {
                return false;
            }
            return true;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var attributeSet = asc.AttributeSet as OrbAttributeSet;
            if (attributeSet != null)
            {
                attributeSet.Mana.ModifyCurrentValue(-20f);
            }

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
