# Move Slow Aura System - Setup Guide

## ✅ Scripts Đã Tạo

1. **MoveSlowAuraAbility.cs** - Ability tạo vùng slow
2. **AuraDetector.cs** - Component detect enemy vào/ra aura
3. **AddMoveSpeedAttribute.cs** - Editor tool để patch FDAttributeSet

## 📝 Các Bước Setup

### Bước 1: Thêm MoveSpeed Attribute

**Cách 1: Dùng Editor Tool (Khuyến Nghị)**
1. Trong Unity menu: **Tools → GAS → Add MoveSpeed Attribute**
2. Check Console xem "✅ FDAttributeSet.cs has been patched"
3. Đợi Unity recompile

**Cách 2: Thủ Công**

Mở `Assets/_Master/Scripts/Base/FDAttributeSet.cs` và thêm:

#### A. Thêm Property (dòng ~19):
```csharp
public GameplayAttribute ManaRegen { get; private set; }
public GameplayAttribute MoveSpeed { get; private set; } // ← THÊM DÒNG NÀY
public GameplayAttribute Armor { get; private set; }
```

#### B. Initialize (dòng ~30):
```csharp
ManaRegen = new GameplayAttribute();
MoveSpeed = new GameplayAttribute(); // ← THÊM DÒNG NÀY
Armor = new GameplayAttribute();
```

#### C. Register (dòng ~39):
```csharp
RegisterAttribute(EGameplayAttributeType.ManaRegen, ManaRegen);
RegisterAttribute(EGameplayAttributeType.MoveSpeed, MoveSpeed); // ← THÊM DÒNG NÀY
RegisterAttribute(EGameplayAttributeType.Armor, Armor);
```

#### D. Set Default (dòng ~44):
```csharp
// Set default values
MoveSpeed.SetBaseValue(5f); // ← THÊM DÒNG NÀY
CriticalMultiplier.SetBaseValue(2f);
```

#### E. Subscribe (dòng ~49):
```csharp
Health.OnValueChanged += OnHealthChanged;
Mana.OnValueChanged += OnManaChanged;
MoveSpeed.OnValueChanged += OnMoveSpeedChanged; // ← THÊM DÒNG NÀY
Armor.OnValueChanged += OnArmorChanged;
```

#### F. Add Callback (sau OnStaminaChanged):
```csharp
private void OnMoveSpeedChanged(float oldValue, float newValue)
{
    Debug.Log($"MoveSpeed changed: {oldValue} -> {newValue}");
}
```

### Bước 2: Tạo SlowEffect (GameplayEffect)

1. **Right-click** trong Project → **Create → GAS → Gameplay Effect**
2. Đặt tên: **SlowEffect**
3. Save vào: `Assets/_Master/Abilities/Effects/`
4. Config SlowEffect:

```
Duration Policy: Infinite
Duration Magnitude: 0

Modifiers (Add Modifier):
  ┌─────────────────────────────────┐
  │ Attribute: MoveSpeed            │
  │ Operation: Multiply             │
  │ Magnitude Type: ScalableFloat   │
  │   └─ Value: 0.5 (50% slow)     │
  └─────────────────────────────────┘

Stacking:
  ┌─────────────────────────────────┐
  │ Stack Limit Policy: LimitedStack│
  │ Stack Limit Count: 5            │
  │ Stack Duration: RenewDuration   │
  │ Stack Period: ResetOnSuccess    │
  └─────────────────────────────────┘
```

**Giải thích:**
- **Multiply 0.5** = Giảm 50% speed (base 5 → 2.5)
- **Stack Limit 5** = Tối đa 5 auras cùng lúc
- **Infinite Duration** = Chỉ remove khi ra khỏi aura

### Bước 3: Tạo MoveSlowAura Ability

1. **Right-click** → **Create → GAS → Abilities → Move Slow Aura**
2. Đặt tên: **MoveSlowAura**
3. Config:

```
Ability Name: "Move Slow Aura"
Description: "Creates an area that slows enemies"

Cooldown Duration: 10 (10 giây cooldown)
Cost Amount: 50 (50 mana)

Aura Settings:
  ┌─────────────────────────────────┐
  │ Aura Radius: 5                  │
  │ Aura Duration: 10               │
  │ Aura Prefab: (None - optional)  │
  │ Follow Caster: false            │
  └─────────────────────────────────┘

Slow Effect:
  ┌─────────────────────────────────┐
  │ Slow Effect: SlowEffect ◄───────┤ Drag SlowEffect vào đây
  └─────────────────────────────────┘
```

### Bước 4: Test Trong Training Scene

1. **Mở BattleTraining scene**
2. **Select TrainingPlayer**
3. **Add MoveSlowAura vào Available Abilities list**
4. **Enter Play Mode**

#### Test Flow:
```
1. Click "Create Enemy" → Spawn DummyEnemy
2. Chọn "MoveSlowAura" trong dropdown
3. Click "Activate Ability"
4. Aura xuất hiện (purple sphere)
5. Di chuyển enemy vào aura
6. Check Console: "✓ Applied slow to DummyEnemy"
7. Check: "→ Current MoveSpeed: 2.50"
8. Di chuyển enemy ra ngoài
9. Check Console: "✗ Removed slow from DummyEnemy"
10. Check: "→ Restored MoveSpeed: 5.00"
```

## 🎯 Features Implemented

### ✅ Auto Apply/Remove Effect
- Enemy vào aura → Auto apply slow
- Enemy ra khỏi aura → Auto remove slow
- Không cần code thêm!

### ✅ Stack Support
- Nhiều aura cùng lúc → Stack effects
- Ví dụ: 2 auras × 0.5 multiply = 0.25 speed (75% slow)
- Max 5 stacks (configurable)

### ✅ Visual Feedback
- Purple transparent sphere hiển thị vùng aura
- Gizmos trong Scene view
- Debug logs khi apply/remove

### ✅ Follow Caster (Optional)
- Tick "Follow Caster" → Aura di chuyển theo player
- Untick → Aura đứng yên tại vị trí spawn

## 📊 Testing Scenarios

### Test 1: Single Aura
```
Enemy enters aura
  → MoveSpeed: 5.0 → 2.5 (50% slow)
Enemy exits aura
  → MoveSpeed: 2.5 → 5.0 (restored)
```

### Test 2: Multiple Auras (Stacking)
```
Enemy vào Aura 1
  → Speed: 5.0 → 2.5
Enemy vào Aura 2 (cùng lúc trong cả 2 auras)
  → Speed: 2.5 → 1.25 (stack: 0.5 × 0.5 = 0.25)
Enemy ra khỏi Aura 1
  → Speed: 1.25 → 2.5 (còn Aura 2)
Enemy ra khỏi Aura 2
  → Speed: 2.5 → 5.0 (restored)
```

### Test 3: Stack Limit
```
Enemy vào 6 auras cùng lúc
  → Chỉ stack tối đa 5 lần
  → Speed không giảm quá mức
```

## 🔧 Customization

### Thay Đổi Slow Amount

**Giảm nhiều hơn (70% slow):**
```
SlowEffect:
  Modifier → Magnitude: 0.3 (giữ 30% speed)
```

**Giảm cố định (-2 speed):**
```
SlowEffect:
  Modifier → Operation: Add
  Modifier → Magnitude: -2
```

### Thay Đổi Aura Visual

Tạo Prefab với Particle System:
```
1. Create Empty GameObject → "SlowAuraPrefab"
2. Add Particle System
   - Shape: Sphere
   - Radius: 5
   - Color: Purple
   - Emission: 50
3. Save as Prefab
4. Assign vào MoveSlowAura → Aura Prefab
```

### Thay Đổi Stack Behavior

**Không giới hạn stack:**
```
SlowEffect:
  Stack Limit Policy: Unlimited
```

**Stack đến 10 lần:**
```
SlowEffect:
  Stack Limit Count: 10
```

## 📖 Code Architecture

```
MoveSlowAuraAbility.OnAbilityActivated()
    ↓
Spawn GameObject với AuraDetector
    ↓
AuraDetector.OnTriggerEnter(enemy)
    ↓
ApplySlowEffect(enemy.ASC)
    ↓
ASC.ApplyGameplayEffectToSelf(SlowEffect)
    ↓
ModifyAttribute(MoveSpeed, Multiply, 0.5)
    ↓
Enemy MoveSpeed = BaseValue * 0.5
    ↓
OnTriggerExit(enemy)
    ↓
RemoveSlowEffect(enemy.ASC)
    ↓
ASC.RemoveGameplayEffect(activeEffect)
    ↓
Enemy MoveSpeed restored to BaseValue
```

## 🐛 Troubleshooting

**Error: MoveSpeed not found**
- Chạy Tools → GAS → Add MoveSpeed Attribute
- Hoặc thêm thủ công vào FDAttributeSet

**Aura không xuất hiện**
- Check Console có error?
- Check Slow Effect đã assign?
- Check TrainingPlayer có ASC?

**Enemy không bị slow**
- Check enemy có BaseCharacter component?
- Check enemy có ASC và AttributeSet?
- Check MoveSpeed BaseValue > 0?

**Effect không remove khi ra khỏi aura**
- Check Collider là Trigger?
- Check enemy có Rigidbody?
- Check OnTriggerExit được gọi?

## 🎊 Done!

Bạn đã có một hệ thống Move Slow Aura hoàn chỉnh với:
- ✅ Auto apply/remove effects
- ✅ Stack support
- ✅ Visual feedback
- ✅ Debug logging
- ✅ Highly customizable

Giờ có thể tạo nhiều loại aura khác: Damage Over Time, Heal Over Time, Buff Aura, etc!
