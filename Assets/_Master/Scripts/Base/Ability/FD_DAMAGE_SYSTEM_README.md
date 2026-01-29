# Farm Defense Damage System - Implementation Guide

## 📋 Tổng quan kiến trúc

Hệ thống damage của Farm Defense sử dụng kiến trúc **generic và extensible**, cho phép mỗi game có thể tùy chỉnh damage calculation riêng mà không làm ảnh hưởng đến base GAS system.

## 🏗️ Kiến trúc phân tầng

```
┌─────────────────────────────────────────────────────┐
│          BASE GAS LAYER (Generic)                   │
├─────────────────────────────────────────────────────┤
│ - GameplayEffectContext (empty base)                │
│ - GameplayAbility (base ability)                    │
│ - DamageCalculationBase (abstract)                  │
│ - GameplayEffectModifier (supports custom calc)     │
└─────────────────────────────────────────────────────┘
                        ▼ Extends
┌─────────────────────────────────────────────────────┐
│       FARM DEFENSE LAYER (FD Specific)              │
├─────────────────────────────────────────────────────┤
│ - FDGameplayEffectContext (+ damageType)            │
│ - FDGameplayAbility (+ damageType, baseDamage)      │
│ - WC3DamageCalculation (WC3 formula)                │
│ - FDAttributeSet (+ armorType, armor, crit)         │
│ - EDamageType, EArmorType enums                     │
└─────────────────────────────────────────────────────┘
```

## 🎯 Data Flow

```
1. FDGameplayAbility activates
   ├─ Has: damageType (Magic, Pierce, etc.)
   └─ Has: baseDamage

2. Creates FDGameplayEffectContext
   ├─ damageType: từ ability
   ├─ baseDamage: từ ability
   ├─ sourceASC: tower/attacker
   └─ targetASC: enemy/defender

3. Applies GameplayEffect
   └─ Modifier uses CustomCalculationClass

4. WC3DamageCalculation.CalculateMagnitude()
   ├─ Cast context to FDGameplayEffectContext
   ├─ Get damageType from context
   ├─ Get armorType from target.FDAttributeSet
   ├─ Step 1: Roll critical (source.CriticalChance)
   ├─ Step 2: Lookup type modifier (damageType vs armorType)
   ├─ Step 3: Calculate armor reduction (target.Armor)
   └─ Return: final damage

5. Apply to target.Health
```

## 📁 Files Created

### Base GAS Layer (Generic)
- `GameplayEffectContext.cs` - Base context (empty, extensible)
- `DamageCalculationBase.cs` - Abstract calculation class
- `GameplayEffect.cs` - Updated to support custom calculation

### FD Specific Layer
- `EDamageType.cs` - Attack types (Normal, Pierce, Siege, Magic, Chaos, Hero)
- `EArmorType.cs` - Armor types (Light, Medium, Heavy, Fortified, etc.)
- `FDGameplayEffectContext.cs` - FD context with damageType
- `FDGameplayAbility.cs` - FD ability with damageType
- `FDAttributeSet.cs` - Updated with armorType, armor, crit attributes
- `WC3DamageCalculation.cs` - WC3 damage formula implementation
- `DamageTypeModifierTable.cs` - Type modifier table
- `FDProjectileAbility.cs` - Example ability

## 🚀 Setup trong Unity Editor

### Step 1: Create Damage Type Table
```
Right-click in Project → Create → FD → Damage Calculation → Damage Type Modifier Table
- Name: "WC3TypeTable"
- Right-click → Initialize Default WC3 Table
- (Optional) Customize modifiers
```

### Step 2: Create WC3 Damage Calculation
```
Right-click → Create → FD → Damage Calculation → WC3 Damage Calculation
- Name: "WC3DamageCalc"
- Assign: Modifier Table → "WC3TypeTable"
- Set: Allow Critical = true
- Set: Debug Log = true (for testing)
```

### Step 3: Create GameplayEffect
```
Right-click → Create → GAS → Gameplay Effect
- Name: "MagicDamageEffect"
- Duration Type: Instant
- Add Modifier:
  * Attribute: Health
  * Operation: Add
  * Calculation Type: CustomCalculationClass
  * Custom Calculation: WC3DamageCalc
  * Scalable Magnitude: -1000 (negative for damage)
```

### Step 4: Create FD Ability
```
Right-click → Create → FD → Abilities → FD Gameplay Ability
- Name: "MagicArrowAbility"
- Damage Type: Magic  ◄── FD specific!
- Base Damage: 1000
- Effect To Apply: MagicDamageEffect
```

### Step 5: Setup Characters

**Tower (Attacker):**
```
GameObject → Add Component → AbilitySystemComponent
- Attribute Set Type: FDAttributeSet

In FDAttributeSet (Inspector):
- Armor Type: Light (doesn't matter for attacker)
- Armor: 5
- Critical Chance: 25 (25% crit chance)
- Critical Multiplier: 2.5 (2.5x damage on crit)
```

**Enemy (Target):**
```
GameObject → Add Component → AbilitySystemComponent
- Attribute Set Type: FDAttributeSet

In FDAttributeSet (Inspector):
- Armor Type: Heavy  ◄── Important!
- Armor: 20
- Health: 5000
- Max Health: 5000
```

## 🎮 Usage Examples

### Example 1: Apply Ability Directly
```csharp
// Get components
var towerASC = tower.GetComponent<AbilitySystemComponent>();
var enemyASC = enemy.GetComponent<AbilitySystemComponent>();

// Give ability to tower
towerASC.GiveAbility(magicArrowAbility);

// Activate on target
var spec = towerASC.GetAbilitySpec(magicArrowAbility);
magicArrowAbility.ActivateAbility(towerASC, spec);
```

### Example 2: Create Custom FD Ability
```csharp
[CreateAssetMenu(fileName = "MyCustomAbility", menuName = "FD/Abilities/My Custom")]
public class MyCustomFDAbility : FDGameplayAbility
{
    protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
    {
        base.OnAbilityActivated(asc, spec);
        
        // Find target
        var target = FindTarget();
        
        // Apply effect with FD context
        if (effectToApply != null && target != null)
        {
            ApplyEffectWithContext(effectToApply, asc, target, spec);
        }
        
        EndAbility(asc, spec);
    }
}
```

### Example 3: Create Custom Calculation for Another Game
```csharp
// For a different game (e.g., MyGame)
public class MyGameEffectContext : GameplayEffectContext
{
    public float ElementalBonus { get; set; }
    public bool IsBurning { get; set; }
}

public class MyGameDamageCalculation : DamageCalculationBase
{
    public override float CalculateMagnitude(
        GameplayEffectContext context,
        AbilitySystemComponent sourceASC,
        AbilitySystemComponent targetASC,
        float baseMagnitude,
        float level)
    {
        var myContext = context as MyGameEffectContext;
        // Custom calculation logic...
    }
}
```

## 📊 Damage Calculation Example

**Scenario:**
- Tower: Magic Arrow (damageType = Magic, baseDamage = 1000)
- Tower: 25% crit chance, 2.5x crit multiplier
- Enemy: Heavy armor, 20 armor value

**Calculation:**
```
Step 1: Critical
- Roll: 0.15 < 0.25 → CRIT!
- Damage: 1000 × 2.5 = 2500

Step 2: Type Modifier (Magic vs Heavy)
- Table lookup: Magic vs Heavy = 2.0
- Damage: 2500 × 2.0 = 5000

Step 3: Armor Reduction (20 armor)
- Reduction: (20 × 0.06) / (1 + 0.06 × 20) = 0.545 (54.5%)
- Damage: 5000 × (1 - 0.545) = 2275

Final: Enemy takes 2275 damage
```

## 🔧 Debugging

### Enable Debug Logs
```
WC3DamageCalculation asset:
- Debug Log: ✓ true
```

### Console Output Example
```
[WC3 Damage] [CRIT x2.5!] Magic vs Heavy (Armor:20) = 2275.0
  Base:1000 x Crit:2.5 x Type:2.00 x (1-Armor:54.5%)
```

### Check Context
```csharp
var context = FDGameplayEffectContext.Current;
Debug.Log($"Damage Type: {context.DamageType}");
Debug.Log($"Is Crit: {context.IsCriticalHit}");
Debug.Log($"Type Modifier: {context.TypeModifier}");
```

## ✅ Key Features

1. **Generic Base Classes** - Không ràng buộc vào game cụ thể
2. **Extensible** - Mỗi game extend context và ability riêng
3. **Type-Safe** - Dùng enums cho damage/armor types
4. **Scriptable Objects** - Easy setup trong Unity Editor
5. **Context Pipeline** - Pass data through calculation cleanly
6. **Debug-Friendly** - Detailed logging for testing

## 🎓 Best Practices

1. **Always extend FDGameplayAbility** (not GameplayAbility) cho FD abilities
2. **Set armorType trên FDAttributeSet** trong Inspector
3. **Set damageType trên FDGameplayAbility** trong Inspector
4. **Assign WC3DamageCalculation** vào GameplayEffect modifiers
5. **Initialize modifier table** trước khi dùng
6. **Enable debug log** khi testing damage

## 🔮 Future Extensions

- Support multiple damage instances (AOE, multi-hit)
- Damage over time (DOT) with type modifiers
- Shield system with type resistance
- Elemental damage types (Fire, Ice, Lightning)
- Armor penetration mechanics
- Damage reduction buffs/debuffs
