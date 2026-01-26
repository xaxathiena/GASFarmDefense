using UnityEngine;

namespace GAS.Example
{
    /// <summary>
    /// Example GameplayEffect configurations demonstrating the enhanced modifier system
    /// </summary>
    public static class GameplayEffectExamples
    {
        /// <summary>
        /// Example 1: Simple flat damage (50 HP)
        /// </summary>
        public static GameplayEffect CreateSimpleDamage()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Simple Damage";
            effect.description = "Deals 50 instant damage";
            effect.durationType = EGameplayEffectDurationType.Instant;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Health),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.ScalableFloat,
                    scalableMagnitude = new ScalableFloat(-50f)
                }
            };
            
            return effect;
        }
        
        /// <summary>
        /// Example 2: AttackPower-based damage (AttackPower * 1.5 + 20)
        /// </summary>
        public static GameplayEffect CreateAttackBasedDamage()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Attack-Based Damage";
            effect.description = "Deals damage based on caster's AttackPower stat";
            effect.durationType = EGameplayEffectDurationType.Instant;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Health),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.AttributeBased,
                    backingAttribute = new AttributeSelector(EGameplayAttributeType.AttackPower),
                    attributeSource = EAttributeSource.Source,
                    coefficient = 1.5f,
                    postMultiplyAdditiveValue = 20f
                }
            };
            
            return effect;
        }
        
        /// <summary>
        /// Example 3: Percentage-based healing (25% of target's max health)
        /// </summary>
        public static GameplayEffect CreatePercentageHealing()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Percentage Heal";
            effect.description = "Heals for 25% of target's max health";
            effect.durationType = EGameplayEffectDurationType.Instant;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Health),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.AttributeBased,
                    backingAttribute = new AttributeSelector(EGameplayAttributeType.Health),
                    attributeSource = EAttributeSource.Target,
                    snapshotAttribute = true,
                    coefficient = 0.25f
                }
            };
            
            return effect;
        }
        
        /// <summary>
        /// Example 4: Speed buff (30% increase for 5 seconds)
        /// </summary>
        public static GameplayEffect CreateSpeedBuff()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Speed Boost";
            effect.description = "Increases movement speed by 30% for 5 seconds";
            effect.durationType = EGameplayEffectDurationType.Duration;
            effect.durationMagnitude = 5f;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.MoveSpeed),
                    operation = EGameplayModifierOp.Multiply,
                    calculationType = EModifierCalculationType.ScalableFloat,
                    scalableMagnitude = new ScalableFloat(1.3f)
                }
            };
            
            effect.grantedTags = new string[] { "Buff.Speed" };
            
            return effect;
        }
        
        /// <summary>
        /// Example 5: Damage over time (10 damage per second for 5 seconds)
        /// </summary>
        public static GameplayEffect CreateDamageOverTime()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Poison";
            effect.description = "Deals 10 damage per second for 5 seconds";
            effect.durationType = EGameplayEffectDurationType.Duration;
            effect.durationMagnitude = 5f;
            effect.isPeriodic = true;
            effect.period = 1f;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Health),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.ScalableFloat,
                    scalableMagnitude = new ScalableFloat(-10f)
                }
            };
            
            effect.grantedTags = new string[] { "Debuff.Poison" };
            
            return effect;
        }
        
        /// <summary>
        /// Example 6: Defense reduction debuff (Reduce defense by 50% for 10 seconds)
        /// </summary>
        public static GameplayEffect CreateArmorReduction()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Defense Break";
            effect.description = "Reduces defense by 50% for 10 seconds";
            effect.durationType = EGameplayEffectDurationType.Duration;
            effect.durationMagnitude = 10f;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Defense),
                    operation = EGameplayModifierOp.Multiply,
                    calculationType = EModifierCalculationType.ScalableFloat,
                    scalableMagnitude = new ScalableFloat(0.5f)
                }
            };
            
            effect.grantedTags = new string[] { "Debuff.DefenseBreak" };
            
            return effect;
        }
        
        /// <summary>
        /// Example 7: Attack power buff (Increases attack power based on level)
        /// </summary>
        public static GameplayEffect CreateCritChanceBuff()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Power Focus";
            effect.description = "Increases attack power (scales with level)";
            effect.durationType = EGameplayEffectDurationType.Duration;
            effect.durationMagnitude = 8f;
            
            // Create a curve that gives +10 attack at level 1, +20 at level 10
            var curve = AnimationCurve.Linear(1, 10, 10, 20);
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.AttackPower),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.ScalableFloat,
                    scalableMagnitude = new ScalableFloat(curve)
                }
            };
            
            effect.grantedTags = new string[] { "Buff.AttackPower" };
            
            return effect;
        }
        
        /// <summary>
        /// Example 8: Lifesteal effect (Heal for 20% of damage dealt)
        /// Use this by setting the damage amount via SetByCaller
        /// </summary>
        public static GameplayEffect CreateLifesteal()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Lifesteal";
            effect.description = "Heals caster for 20% of damage dealt";
            effect.durationType = EGameplayEffectDurationType.Instant;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Health),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.SetByCaller,
                    setByCallerTag = "Damage.Dealt",
                    coefficient = 0.2f // 20% of damage
                }
            };
            
            return effect;
        }
        
        /// <summary>
        /// Example 9: Stamina boost based on max health (Stamina = 30% of max health)
        /// </summary>
        public static GameplayEffect CreateHealthShield()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Stamina Boost";
            effect.description = "Grants stamina equal to 30% of max health for 15 seconds";
            effect.durationType = EGameplayEffectDurationType.Duration;
            effect.durationMagnitude = 15f;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Stamina),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.AttributeBased,
                    backingAttribute = new AttributeSelector(EGameplayAttributeType.MaxHealth),
                    attributeSource = EAttributeSource.Target,
                    snapshotAttribute = true,
                    coefficient = 0.3f
                }
            };
            
            effect.grantedTags = new string[] { "Buff.Stamina" };
            
            return effect;
        }
        
        /// <summary>
        /// Example 10: Combo multiplier (Damage = Base * AttackPower)
        /// </summary>
        public static GameplayEffect CreateComboMultiplier()
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Combo Damage";
            effect.description = "Damage scales with attack power";
            effect.durationType = EGameplayEffectDurationType.Instant;
            
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Health),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.AttributeBased,
                    backingAttribute = new AttributeSelector(EGameplayAttributeType.AttackPower),
                    attributeSource = EAttributeSource.Source,
                    preMultiplyAdditiveValue = 50f, // Base 50 damage
                    coefficient = 1f, // Plus 1x AttackPower
                }
            };
            
            return effect;
        }
    }
}
