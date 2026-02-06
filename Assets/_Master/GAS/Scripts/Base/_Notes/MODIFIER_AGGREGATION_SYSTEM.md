# Modifier Aggregation System

## Tổng Quan

Hệ thống **Modifier Aggregation** giải quyết vấn đề quan trọng: **Làm sao remove GameplayEffect mà vẫn tính đúng attribute value?**

### Vấn Đề Trước Đây

```csharp
// System cũ: Apply trực tiếp lên CurrentValue
BaseValue = 100

1. Apply Effect A: Multiply 0.5
   CurrentValue = 100 * 0.5 = 50 ✓

2. Apply Effect B: Add 20
   CurrentValue = 50 + 20 = 70 ✓

3. Remove Effect A:
   CurrentValue = ??? (VẪN LÀ 70 - WRONG!)
   Đáng lẽ phải là: (100 + 20) = 120
```

**Vấn đề:** Không có cách nào "undo" modifier khi remove effect!

### Giải Pháp: Modifier Aggregator

Thay vì apply trực tiếp, **track tất cả modifiers** và **recalculate từ BaseValue** mỗi khi có thay đổi.

```csharp
// System mới: Track modifiers
BaseValue = 100
Modifiers = [
    { Effect: A, Op: Multiply, Mag: 0.5 },
    { Effect: B, Op: Add, Mag: 20 }
]

CurrentValue = CalculateFinalValue(BaseValue)
             = (100 + 20) * 0.5  // Add trước, Multiply sau
             = 60 ✓

Remove Effect A:
Modifiers = [
    { Effect: B, Op: Add, Mag: 20 }
]
CurrentValue = (100 + 20) = 120 ✓ // Recalculate đúng!
```

---

## Kiến Trúc

### 1. AttributeModifier

Đại diện cho một modifier entry:

```csharp
public class AttributeModifier
{
    public ActiveGameplayEffect SourceEffect;  // Effect nào tạo ra modifier này
    public EGameplayModifierOp Operation;      // Add, Multiply, Divide, Override
    public float Magnitude;                     // Giá trị modifier
    public float ApplyTime;                     // Thời điểm apply (để sort)
}
```

### 2. AttributeModifierAggregator

Quản lý danh sách modifiers và tính final value:

```csharp
public class AttributeModifierAggregator
{
    private List<AttributeModifier> modifiers;

    // Add modifier khi effect được apply
    void AddModifier(ActiveGameplayEffect effect, EGameplayModifierOp op, float magnitude);

    // Remove modifiers khi effect bị remove
    void RemoveModifiersFromEffect(ActiveGameplayEffect effect);

    // Tính final value theo thứ tự: Base → Add → Multiply → Divide
    float CalculateFinalValue(float baseValue);
}
```

**Execution Order:**
1. **Base Value**: Giá trị permanent
2. **Add Operations**: Tất cả Add modifiers (+/-) apply trước
3. **Multiply Operations**: Sau đó Multiply (%)
4. **Divide Operations**: Cuối cùng Divide
5. **Override**: Nếu có, override wins (last one applied)

### 3. GameplayAttribute

Updated với aggregation support:

```csharp
public class GameplayAttribute
{
    private float baseValue;
    private float currentValue;
    private AttributeModifierAggregator aggregator;
    private bool isDirty;

    public float CurrentValue
    {
        get
        {
            if (isDirty) RecalculateCurrentValue();
            return currentValue;
        }
    }

    // Add modifier từ Duration/Infinite effect
    public void AddModifier(ActiveGameplayEffect effect, EGameplayModifierOp op, float magnitude)
    {
        aggregator.AddModifier(effect, op, magnitude);
        RecalculateCurrentValue();
    }

    // Remove modifiers khi effect removed
    public void RemoveModifiersFromEffect(ActiveGameplayEffect effect)
    {
        aggregator.RemoveModifiersFromEffect(effect);
        RecalculateCurrentValue();
    }

    // Recalculate từ BaseValue và tất cả modifiers
    void RecalculateCurrentValue()
    {
        currentValue = aggregator.CalculateFinalValue(baseValue);
        // Apply clamping...
    }
}
```

---

## Effect Types & Behavior

### Instant Effects

Apply **trực tiếp vào BaseValue** (permanent change):

```csharp
// Damage: -50 Health (instant)
effect.durationType = EGameplayEffectDurationType.Instant;
modifier.operation = EGameplayModifierOp.Add;
modifier.magnitude = -50f;

// Apply:
attribute.ModifyBaseValue(-50f); // BaseValue permanent giảm 50
```

**Không qua aggregator** vì instant = permanent change.

### Duration/Infinite Effects

Add vào **aggregator** (temporary change):

```csharp
// Slow: -30% MoveSpeed (10 seconds)
effect.durationType = EGameplayEffectDurationType.Duration;
effect.durationMagnitude = 10f;
modifier.operation = EGameplayModifierOp.Multiply;
modifier.magnitude = 0.7f; // 70% speed

// Apply:
attribute.AddModifier(activeEffect, EGameplayModifierOp.Multiply, 0.7f);
// CurrentValue = (BaseValue + adds) * 0.7

// After 10 seconds hoặc manual remove:
attribute.RemoveModifiersFromEffect(activeEffect);
// CurrentValue recalculated without 0.7 multiplier
```

### Periodic Effects

Apply giống **Instant** (mỗi period apply vào BaseValue):

```csharp
// Damage Over Time: -10 HP every 1 second
effect.isPeriodic = true;
effect.period = 1f;
modifier.magnitude = -10f;

// Mỗi giây:
attribute.ModifyBaseValue(-10f); // Permanent damage
```

---

## Execution Order Examples

### Example 1: Add + Multiply

```
BaseValue: 100

Effect A: Add +20
Effect B: Multiply *0.5

Execution:
  Step 1 (Base): 100
  Step 2 (Add): 100 + 20 = 120
  Step 3 (Multiply): 120 * 0.5 = 60

CurrentValue = 60
```

### Example 2: Multiple Adds + Multiply

```
BaseValue: 100

Effect A: Add +20
Effect B: Add +10
Effect C: Multiply *0.8

Execution:
  Base: 100
  Adds: 100 + 20 + 10 = 130
  Multiply: 130 * 0.8 = 104

CurrentValue = 104
```

### Example 3: Remove Middle Effect

```
BaseValue: 100

Apply:
  Effect A: Add +20
  Effect B: Multiply *0.5
  Effect C: Add +10

CurrentValue = (100 + 20 + 10) * 0.5 = 65

Remove Effect B:
CurrentValue = 100 + 20 + 10 = 130 ✓ (Recalculated correctly!)
```

### Example 4: Stacking

```
BaseValue: 100

Effect (stackable): Add +10

Apply 1st stack:
  Modifiers: [+10]
  CurrentValue = 100 + 10 = 110

Apply 2nd stack (same effect):
  StackCount = 2
  Modifiers: [+20] // Magnitude * StackCount
  CurrentValue = 100 + 20 = 120

Apply 3rd stack:
  StackCount = 3
  Modifiers: [+30]
  CurrentValue = 100 + 30 = 130
```

---

## Implementation Details

### 1. Tracking Affected Attributes

```csharp
// ActiveGameplayEffect
private List<GameplayAttribute> affectedAttributes;

public void AddAffectedAttribute(GameplayAttribute attr)
{
    affectedAttributes.Add(attr);
}

// Khi effect được apply:
attribute.AddModifier(activeEffect, op, magnitude);
activeEffect.AddAffectedAttribute(attribute); // Track để cleanup sau
```

### 2. Remove Effect

```csharp
// AbilitySystemComponent.RemoveGameplayEffect()
public void RemoveGameplayEffect(ActiveGameplayEffect effect)
{
    // Remove modifiers from all affected attributes
    foreach (var attr in effect.GetAffectedAttributes())
    {
        attr.RemoveModifiersFromEffect(effect); // ← Triggers recalculation
    }

    // Remove tags
    RemoveTags(effect.Effect.grantedTags);

    // Remove from list
    activeGameplayEffects.Remove(effect);
}
```

### 3. Apply Effect (Updated)

```csharp
// GameplayEffect.ApplyModifierWithAggregation()
public void ApplyModifierWithAggregation(
    AttributeSet targetAttributeSet,
    GameplayEffectModifier modifier,
    AbilitySystemComponent sourceASC,
    AbilitySystemComponent targetASC,
    float level,
    float stackCount,
    ActiveGameplayEffect activeEffect,
    bool isInstant)
{
    var attribute = targetAttributeSet.GetAttribute(modifier.GetAttributeName());
    float magnitude = modifier.CalculateMagnitude(...);

    if (isInstant)
    {
        // Apply to BaseValue (permanent)
        switch (modifier.operation)
        {
            case Add: attribute.ModifyBaseValue(magnitude); break;
            case Multiply: attribute.SetBaseValue(BaseValue * magnitude); break;
            // ...
        }
    }
    else
    {
        // Add to aggregator (temporary)
        attribute.AddModifier(activeEffect, modifier.operation, magnitude);
        activeEffect.AddAffectedAttribute(attribute);
    }
}
```

---

## Passive Abilities Integration

### Stacking Policy

Passive abilities có thể config stacking:

```csharp
// GameplayEffect
[Header("Stacking")]
public bool allowStacking = false;
public int maxStacks = 1;
public bool refreshDurationOnStack = true;
```

**Item System Example:**

```csharp
// Item 1: "Boots of Speed" (+20 MoveSpeed)
// Item 2: "Boots of Speed" (cùng loại)

Item 1 equipped:
  ASC.ApplyGameplayEffectToSelf(bootsEffect);
  CurrentValue = BaseValue + 20

Item 2 equipped:
  If allowStacking = true:
    StackCount = 2
    CurrentValue = BaseValue + (20 * 2) = BaseValue + 40
  
  If allowStacking = false:
    Không apply thêm (hoặc replace existing)
```

### Add/Remove Passives

```csharp
// Item equip
public void OnItemEquip(Item item)
{
    if (item.passiveAbility != null)
    {
        // Grant ability
        var spec = ASC.GiveAbility(item.passiveAbility);
        
        // Activate (apply effect)
        ASC.TryActivateAbility(spec);
        
        // Store reference for unequip
        item.activeEffectHandle = /* store reference */;
    }
}

// Item unequip
public void OnItemUnequip(Item item)
{
    if (item.activeEffectHandle != null)
    {
        // Remove effect → Modifiers removed → Recalculate
        ASC.RemoveGameplayEffect(item.activeEffectHandle);
    }
}
```

---

## Testing

### Test Script

Sử dụng `ModifierAggregationTest.cs` để verify:

```
Test 1: Add và Remove single effect
  ✓ CurrentValue restore về BaseValue

Test 2: Multiple effects execution order
  ✓ Add trước, Multiply sau

Test 3: Stacking effects
  ✓ Magnitude * StackCount

Test 4: Remove middle effect
  ✓ Recalculate đúng với effects còn lại
```

### Manual Testing

1. **Tạo test character với ASC**
2. **Tạo test effects:**
   - AddEffect: +20 MoveSpeed
   - MultiplyEffect: *0.5 MoveSpeed (slow 50%)
   - StackingEffect: +10 MoveSpeed (allowStacking=true, maxStacks=5)
3. **Add ModifierAggregationTest component**
4. **Assign effects và character**
5. **Play mode → Check Console logs**

---

## Performance Considerations

### Caching

```csharp
private bool isDirty = false; // Flag để biết cần recalculate

public float CurrentValue
{
    get
    {
        if (isDirty) // Chỉ recalculate khi dirty
        {
            RecalculateCurrentValue();
        }
        return currentValue;
    }
}
```

**Khi nào set dirty:**
- `AddModifier()` → isDirty = true
- `RemoveModifiersFromEffect()` → isDirty = true
- `SetBaseValue()` → Recalculate ngay (affect aggregation)

### Complexity

- **Add Modifier**: O(1) - Add vào list
- **Remove Modifiers**: O(n) - RemoveAll với predicate
- **Calculate Final Value**: O(n) - Iterate qua tất cả modifiers

**n = số lượng modifiers** (thường < 10 per attribute)

→ Performance impact rất nhỏ!

---

## Migration Guide

### Old Code → New Code

**Old:**
```csharp
// Apply effect
effect.ApplyModifiers(targetAttributeSet, source, target, level, stackCount);

// Remove effect
activeGameplayEffects.Remove(effect); // Không restore value!
```

**New:**
```csharp
// Apply effect
foreach (var modifier in effect.modifiers)
{
    effect.ApplyModifierWithAggregation(
        targetAttributeSet, modifier, source, target, 
        level, stackCount, activeEffect, isInstant);
}

// Remove effect
foreach (var attr in activeEffect.GetAffectedAttributes())
{
    attr.RemoveModifiersFromEffect(activeEffect); // Auto recalculate!
}
```

**Backward Compatibility:**

Old `ApplyModifiers()` method vẫn tồn tại cho legacy code, nhưng **không nên dùng** nữa.

---

## Best Practices

### 1. Duration vs Instant

- **Duration/Infinite**: Dùng cho buffs/debuffs temporary → Vào aggregator
- **Instant**: Dùng cho damage/heal permanent → Apply vào BaseValue

### 2. Modifier Operations

- **Add**: Flat bonus/penalty (+/-)
- **Multiply**: Percentage changes (slow, buff)
- **Divide**: Rare (damage mitigation?)
- **Override**: Very rare (set to specific value)

### 3. Execution Order

Luôn nhớ: **Add → Multiply → Divide**

Ví dụ tính damage:
```
Base Damage: 100
+Flat Damage (Add): +50
+Crit (Multiply): *2.0
-Armor (Multiply): *0.8

Final = ((100 + 50) * 2.0) * 0.8 = 240
```

### 4. Stacking

- Stackable effects: `allowStacking=true`, set `maxStacks`
- Non-stackable: Mỗi source chỉ apply 1 instance

---

## Summary

✅ **Vấn đề giải quyết:**
- Remove effect giờ tính lại đúng attribute value
- Multiple effects apply theo thứ tự cố định
- Stacking effects work correctly
- Passive abilities có thể add/remove runtime

✅ **Execution Order:**
- Base → Add → Multiply → Divide → Override

✅ **Effect Types:**
- **Instant**: BaseValue (permanent)
- **Duration/Infinite**: Aggregator (temporary)
- **Periodic**: BaseValue mỗi period

✅ **Performance:**
- Minimal overhead (O(n) với n < 10)
- Caching với dirty flag
- Recalculate chỉ khi cần

🎯 **Ready for production!**
