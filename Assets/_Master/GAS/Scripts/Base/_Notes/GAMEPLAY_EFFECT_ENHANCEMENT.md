# Gameplay Effect System Enhancement

## Overview
Enhanced the Gameplay Effect system to align with Unreal Engine's Gameplay Ability System (GAS), adding advanced modifier calculation types, source/target attribute support, and custom editors.

## New Features

### 1. Modifier Calculation Types
Implemented four calculation types matching Unreal's GAS:

#### **ScalableFloat** (Default)
- Uses the existing `ScalableFloat` system
- Supports flat values, curves, or attribute-based scaling
- Perfect for level-based ability scaling

```csharp
// Example: Simple damage that scales with level
modifier.calculationType = EModifierCalculationType.ScalableFloat;
modifier.scalableMagnitude = new ScalableFloat(10f); // Base 10 damage
```

#### **Attribute Based**
- Calculate magnitude from source (caster) or target (receiver) attributes
- Supports snapshotting (capture at creation vs. application)
- Flexible formula: `(AttributeValue + PreAdd) * Coefficient + PostAdd`

```csharp
// Example: Damage based on caster's Attack stat
modifier.calculationType = EModifierCalculationType.AttributeBased;
modifier.backingAttribute = new AttributeSelector(EGameplayAttributeType.Attack);
modifier.attributeSource = EAttributeSource.Source; // From caster
modifier.coefficient = 1.5f; // 150% of Attack
modifier.postMultiplyAdditiveValue = 20f; // +20 bonus damage
// Result: (Attack * 1.5) + 20
```

```csharp
// Example: Healing based on target's max health
modifier.calculationType = EModifierCalculationType.AttributeBased;
modifier.backingAttribute = new AttributeSelector(EGameplayAttributeType.Health);
modifier.attributeSource = EAttributeSource.Target; // From target
modifier.coefficient = 0.25f; // 25% of max health
// Result: MaxHealth * 0.25
```

#### **SetByCaller** (Placeholder)
- Runtime-defined magnitudes via GameplayTag
- Useful for variable damage (e.g., charge time)
- Full implementation requires GameplayEffectSpec system

```csharp
// Example: Damage based on button hold time
modifier.calculationType = EModifierCalculationType.SetByCaller;
modifier.setByCallerTag = "Ability.Damage.Charged";
// Runtime: spec.SetSetByCaller("Ability.Damage.Charged", chargeTime * 50f);
```

#### **Custom Calculation Class** (Future)
- Placeholder for complex calculations
- For advanced formulas and game-specific logic

### 2. Enhanced Operations
Added **Divide** operation to complement Add, Multiply, and Override:

```csharp
public enum EGameplayModifierOp
{
    Add,      // Add to current value
    Multiply, // Multiply current value
    Divide,   // Divide current value (NEW)
    Override  // Set to new value
}
```

### 3. Source/Target System

#### **EAttributeSource Enum**
```csharp
public enum EAttributeSource
{
    Source,  // From the ability's owner (caster)
    Target   // From the target receiving the effect
}
```

This allows effects to scale based on either:
- **Source attributes**: Caster's stats (Attack, CritChance, etc.)
- **Target attributes**: Receiver's stats (MaxHealth, Armor, etc.)

#### **Snapshotting**
- **Snapshot = true**: Captures attribute value when effect is **created**
- **Snapshot = false**: Captures attribute value when effect is **applied**
- Important for DOTs and delayed effects

### 4. Advanced Modifier Formula
For Attribute-Based calculations:

```
FinalValue = (BackingAttributeValue + PreMultiplyAdditiveValue) * Coefficient + PostMultiplyAdditiveValue
```

**Example Use Cases:**

1. **Simple percentage**: Heal for 30% of caster's max health
   - Backing: Health (Source)
   - Coefficient: 0.3
   
2. **Scaling with bonus**: Damage = Attack * 2 + 50
   - Backing: Attack (Source)
   - Coefficient: 2.0
   - PostAdd: 50

3. **Complex formula**: Damage = (Attack + 10) * 1.5 + 25
   - Backing: Attack (Source)
   - PreAdd: 10
   - Coefficient: 1.5
   - PostAdd: 25

### 5. Custom Editors

#### **GameplayEffectModifierDrawer**
- Conditional UI based on calculation type
- Only shows relevant fields for each type
- Clean, organized property layout

#### **GameplayEffectEditor**
- Collapsible sections for better organization
- Contextual help boxes
- Visual feedback and warnings
- Unreal GAS-inspired layout

## Usage Examples

### Example 1: Simple Damage Effect
```csharp
// Create a 50 damage instant effect
GameplayEffect damageEffect = CreateInstance<GameplayEffect>();
damageEffect.durationType = EGameplayEffectDurationType.Instant;
damageEffect.modifiers = new GameplayEffectModifier[]
{
    new GameplayEffectModifier
    {
        attribute = new AttributeSelector(EGameplayAttributeType.Health),
        operation = EGameplayModifierOp.Add,
        calculationType = EModifierCalculationType.ScalableFloat,
        scalableMagnitude = new ScalableFloat(-50f) // Negative for damage
    }
};
```

### Example 2: Attack-Based Damage
```csharp
// Damage = Caster's Attack * 1.5 + 20
var modifier = new GameplayEffectModifier
{
    attribute = new AttributeSelector(EGameplayAttributeType.Health),
    operation = EGameplayModifierOp.Add,
    calculationType = EModifierCalculationType.AttributeBased,
    backingAttribute = new AttributeSelector(EGameplayAttributeType.Attack),
    attributeSource = EAttributeSource.Source,
    coefficient = 1.5f,
    postMultiplyAdditiveValue = 20f
};
```

### Example 3: Armor Reduction Debuff
```csharp
// Reduce target's armor by 30%
var modifier = new GameplayEffectModifier
{
    attribute = new AttributeSelector(EGameplayAttributeType.Armor),
    operation = EGameplayModifierOp.Multiply,
    calculationType = EModifierCalculationType.ScalableFloat,
    scalableMagnitude = new ScalableFloat(0.7f) // 70% of original
};
```

### Example 4: Percentage-Based Healing
```csharp
// Heal for 25% of target's max health
var modifier = new GameplayEffectModifier
{
    attribute = new AttributeSelector(EGameplayAttributeType.Health),
    operation = EGameplayModifierOp.Add,
    calculationType = EModifierCalculationType.AttributeBased,
    backingAttribute = new AttributeSelector(EGameplayAttributeType.Health),
    attributeSource = EAttributeSource.Target,
    snapshotAttribute = true, // Use max health
    coefficient = 0.25f
};
```

## API Changes

### Updated ApplyModifiers Method
```csharp
// New signature with source/target support
public void ApplyModifiers(
    AttributeSet targetAttributeSet, 
    AbilitySystemComponent sourceASC = null, 
    AbilitySystemComponent targetASC = null, 
    float level = 1f, 
    float stackCount = 1f)

// Legacy signature still supported
public void ApplyModifiers(AttributeSet attributeSet, float stackCount = 1f)
```

### New Methods
```csharp
// Calculate magnitude with full context
float CalculateMagnitude(
    AbilitySystemComponent sourceASC, 
    AbilitySystemComponent targetASC, 
    float level, 
    float stackCount = 1f)
```

## Migration Guide

### For Existing Effects
Existing `GameplayEffect` assets will continue to work:
- Old `magnitude` field is still supported
- Defaults to `ScalableFloat` calculation type
- Backward compatible with existing code

### To Use New Features
1. Set `calculationType` to desired type
2. For `ScalableFloat`: Configure `scalableMagnitude`
3. For `AttributeBased`: Set backing attribute, source, and coefficients
4. For `SetByCaller`: Set the gameplay tag

## Technical Notes

### Divide Operation Safety
```csharp
case EGameplayModifierOp.Divide:
    if (finalMagnitude != 0f)
    {
        targetAttribute.SetCurrentValue(current / finalMagnitude);
    }
    else
    {
        Debug.LogWarning("Attempted to divide attribute by zero!");
    }
    break;
```

### Backward Compatibility
The system maintains compatibility by:
1. Keeping the old `magnitude` field
2. Falling back to legacy calculation if new system returns 0
3. Supporting both old and new ApplyModifiers signatures

## Future Enhancements

1. **GameplayEffectSpec System**
   - Proper snapshotting implementation
   - SetByCaller runtime values
   - Effect context data

2. **Custom Calculation Classes**
   - Inherit from `ModifierMagnitudeCalculation`
   - Complex formulas and game logic
   - Reusable calculation components

3. **Effect Prediction**
   - Client-side effect prediction
   - Rollback and correction

4. **Effect Queries**
   - Query active effects by tag
   - Aggregate modifier values
   - Effect dependency system

## References
- Based on Unreal Engine's Gameplay Ability System
- Compatible with existing `ScalableFloat` system
- Follows Unity's ScriptableObject pattern
