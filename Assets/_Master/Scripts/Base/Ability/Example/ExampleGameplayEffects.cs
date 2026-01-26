using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Example: Damage effect
    /// </summary>
    [CreateAssetMenu(fileName = "GE_Damage", menuName = "GAS/Effects/Damage Effect")]
    public class DamageEffect : GameplayEffect
    {
        // Pre-configured as damage effect
        private void OnValidate()
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                modifiers = new GameplayEffectModifier[]
                {
                    new GameplayEffectModifier(EGameplayAttributeType.Health, EGameplayModifierOp.Add, -10f)
                };
            }
        }
    }
    
    /// <summary>
    /// Example: Healing effect
    /// </summary>
    [CreateAssetMenu(fileName = "GE_Heal", menuName = "GAS/Effects/Heal Effect")]
    public class HealEffect : GameplayEffect
    {
        private void OnValidate()
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                modifiers = new GameplayEffectModifier[]
                {
                    new GameplayEffectModifier(EGameplayAttributeType.Health, EGameplayModifierOp.Add, 20f)
                };
            }
        }
    }
    
    /// <summary>
    /// Example: Damage over time effect
    /// </summary>
    [CreateAssetMenu(fileName = "GE_DOT", menuName = "GAS/Effects/Damage Over Time")]
    public class DamageOverTimeEffect : GameplayEffect
    {
        private void OnValidate()
        {
            durationType = EGameplayEffectDurationType.Duration;
            isPeriodic = true;
            period = 1f;
            durationMagnitude = 5f;
            
            if (modifiers == null || modifiers.Length == 0)
            {
                modifiers = new GameplayEffectModifier[]
                {
                    new GameplayEffectModifier(EGameplayAttributeType.Health, EGameplayModifierOp.Add, -5f)
                };
            }
        }
    }
    
    /// <summary>
    /// Example: Buff effect (increased attack power)
    /// </summary>
    [CreateAssetMenu(fileName = "GE_Buff", menuName = "GAS/Effects/Buff Effect")]
    public class BuffEffect : GameplayEffect
    {
        private void OnValidate()
        {
            durationType = EGameplayEffectDurationType.Duration;
            durationMagnitude = 10f;
            
            if (grantedTags == null || grantedTags.Length == 0)
            {
                grantedTags = new string[] { "State.Buffed" };
            }
            
            if (modifiers == null || modifiers.Length == 0)
            {
                modifiers = new GameplayEffectModifier[]
                {
                    new GameplayEffectModifier(EGameplayAttributeType.AttackPower, EGameplayModifierOp.Add, 10f)
                };
            }
        }
    }
    
    /// <summary>
    /// Example: Stun effect
    /// </summary>
    [CreateAssetMenu(fileName = "GE_Stun", menuName = "GAS/Effects/Stun Effect")]
    public class StunEffect : GameplayEffect
    {
        private void OnValidate()
        {
            durationType = EGameplayEffectDurationType.Duration;
            durationMagnitude = 2f;
            
            if (grantedTags == null || grantedTags.Length == 0)
            {
                grantedTags = new string[] { "State.Stunned", "State.CannotMove", "State.CannotAttack" };
            }
        }
    }
}
