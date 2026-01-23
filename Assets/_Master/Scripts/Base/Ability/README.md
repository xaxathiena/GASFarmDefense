# GAS Ability System for Unity

Simple implementation of Unreal's Gameplay Ability System (GAS) for Unity.

## Core Components

### 1. GameplayAbility (Base Class)
- Abstract base class for all abilities
- Handles cooldowns, costs, and tags
- Override `OnAbilityActivated()` to implement ability logic

### 2. AbilitySystemComponent
- Attach to GameObjects that need abilities
- Manages granted abilities, cooldowns, and resources
- Handles gameplay tags and gameplay effects

### 3. GameplayEffect
- Modifies attributes (damage, healing, buffs, debuffs)
- Three duration types: Instant, Duration, Infinite
- Supports periodic execution (DOT/HOT)
- Tag-based requirements and blocking
- Stacking support

### 4. AttributeSet
- Manages character attributes (Health, Mana, Stamina, etc.)
- Automatic clamping and change notifications

### 5. GameplayAttribute
- Individual attribute with base/current values
- Min/max clamping
- Change events

## How to Use

### Creating a New Ability

1. Create a new script inheriting from `GameplayAbility`:

```csharp
[CreateAssetMenu(fileName = "New Fire Ability", menuName = "GAS/Abilities/Fire Ability")]
public class FireAbility : GameplayAbility
{
    public GameplayEffect burnEffect;
    
    protected override void OnAbilityActivated()
    {
        // Apply burn effect to nearby enemies
        Collider[] enemies = Physics.OverlapSphere(owner.transform.position, 5f);
        
        foreach (var enemy in enemies)
        {
            var targetASC = enemy.GetComponent<AbilitySystemComponent>();
            if (targetASC != null)
            {
                ownerASC.ApplyGameplayEffectToTarget(burnEffect, targetASC, ownerASC);
            }
        }
        
        EndAbility();
    }
}
```

2. Create a ScriptableObject asset:
   - Right-click in Project → Create → GAS → Abilities → [Your Ability]

3. Configure the ability properties:
   - Name, description
   - Cooldown duration
   - Cost amount
   - Tags

### Creating Gameplay Effects

1. Create gameplay effect asset:
   - Right-click → Create → GAS → Gameplay Effect
   
2. Configure the effect:

```plaintext
Duration Type:
- Instant: Applies once (damage, heal)
- Duration: Lasts X seconds (buffs, debuffs)
- Infinite: Permanent until removed

Modifiers:
- Attribute Name: "Health", "Mana", "AttackPower", etc.
- Operation: Add, Multiply, Override
- Magnitude: Value to apply

Periodic:
- Check isPeriodic for DOT/HOT effects
- Set period (tick rate in seconds)

Tags:
- Granted Tags: Added while effect is active
- Required Tags: Target must have these
- Blocked Tags: Prevent application if target has these
```

### Using the System

1. Setup GameObject:

```csharp
public class PlayerController : MonoBehaviour
{
    private AbilitySystemComponent asc;
    
    [Header("Effects")]
    public GameplayEffect healEffect;
    public GameplayEffect damageEffect;
    
    void Start()
    {
        asc = GetComponent<AbilitySystemComponent>();
        
        // Access attributes
        var attributes = asc.AttributeSet;
        Debug.Log($"Health: {attributes.Health.CurrentValue}");
    }
    
    void Update()
    {
        // Apply effect on key press
        if (Input.GetKeyDown(KeyCode.H))
        {
            asc.ApplyGameplayEffectToSelf(healEffect);
        }
        
        // Check active effects
        var activeEffects = asc.GetActiveGameplayEffects();
        foreach (var effect in activeEffects)
        {
            Debug.Log(effect.ToString());
        }
    }
}
```

### Example Effects Included

1. **DamageEffect** - Instant damage
2. **HealEffect** - Instant healing
3. **DamageOverTimeEffect** - Periodic damage (5 seconds, 5 damage per second)
4. **BuffEffect** - Temporary attack boost (10 seconds)
5. **StunEffect** - Disable movement/attacks (2 seconds)

## Key Features

- ✅ Cooldown system
- ✅ Resource/Cost system
- ✅ Gameplay tags
- ✅ Ability cancellation
- ✅ Tag-based ability blocking
- ✅ Attribute system (Health, Mana, Stamina, etc.)
- ✅ Gameplay effects (Instant, Duration, Infinite)
- ✅ Periodic effects (DOT/HOT)
- ✅ Effect stacking
- ✅ Tag-based effect requirements
- ✅ Simple and extensible

## Common Workflows

### Applying Damage
```csharp
// Method 1: Direct attribute modification
asc.AttributeSet.TakeDamage(10f);

// Method 2: Using gameplay effect
asc.ApplyGameplayEffectToSelf(damageEffect);
```

### Creating a Buff
```csharp
// Create effect asset with:
// - Duration: 10 seconds
// - Modifier: AttackPower +20
// - Tags: "State.Buffed"
asc.ApplyGameplayEffectToSelf(attackBuffEffect);
```

### Creating DOT (Damage Over Time)
```csharp
// Create effect asset with:
// - Duration: 5 seconds
// - Periodic: true, Period: 1s
// - Modifier: Health -5
asc.ApplyGameplayEffectToTarget(burnEffect, targetASC, ownerASC);
```

## Example Tags

- "Ability.Attack"
- "Ability.Defense"
- "Ability.Magic"
- "State.Stunned"
- "State.Invulnerable"
- "State.Buffed"
- "State.Dead"

Use tags to control ability interactions and character states.
