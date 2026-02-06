# Gameplay Tag System - Quick Reference

## Basic Usage

### Adding Tags
```csharp
asc.AddTags(GameplayTag.State_Stunned);
asc.AddTags(GameplayTag.State_Burning, GameplayTag.Debuff_Slow);
```

### Removing Tags
```csharp
asc.RemoveTags(GameplayTag.State_Stunned);
asc.RemoveTags(GameplayTag.State_Burning, GameplayTag.Debuff_Slow);
```

### Checking Tags
```csharp
// Has ANY tag (OR logic)
bool stunned = asc.HasAnyTags(GameplayTag.State_Stunned);
bool immune = asc.HasAnyTags(GameplayTag.State_Immune, GameplayTag.State_Immune_CC);

// Has ALL tags (AND logic)
bool wetAndShocked = asc.HasAllTags(GameplayTag.State_Wet, GameplayTag.State_Shocked);
```

## Common Patterns

### Status Check
```csharp
public bool IsStunned() => asc.HasAnyTags(GameplayTag.State_Stunned);
public bool IsDead() => asc.HasAnyTags(GameplayTag.State_Dead);
public bool CanMove() => !asc.HasAnyTags(GameplayTag.State_CannotMove, GameplayTag.State_Stunned);
```

### Immunity Check
```csharp
private bool IsImmune(AbilitySystemComponent target)
{
    return target.HasAnyTags(
        GameplayTag.State_Immune,
        GameplayTag.State_Immune_CC,
        GameplayTag.State_Invulnerable
    );
}
```

### GameplayEffect Setup
```csharp
[CreateAssetMenu(menuName = "GAS/My Effect")]
public class MyEffect : GameplayEffect
{
    private void OnValidate()
    {
        grantedTags = new GameplayTag[] { GameplayTag.State_Burning };
        applicationRequiredTags = new GameplayTag[] { GameplayTag.State_Wet };
        applicationBlockedByTags = new GameplayTag[] { GameplayTag.State_Immune };
    }
}
```

## Available Tags

### State Tags
- State_Stunned, State_Dead, State_Immune
- State_Immune_CC, State_Immune_Stun
- State_Disabled, State_Silenced, State_Invulnerable
- State_CannotMove, State_CannotAttack
- State_Burning, State_Shocked, State_Wet, State_Frozen, State_Poisoned

### Buff/Debuff Tags
- Buff_Speed, Buff_Attack, Buff_Stamina, Buff_Defense
- Debuff_Poison, Debuff_DefenseBreak, Debuff_Slow

### Ability Tags
- Ability_Attack, Ability_Defense, Ability_Magic

### Custom Tags
- Custom_Start = 100 (add your own from here)

## Adding New Tags

Edit `GameplayTag.cs`:
```csharp
public enum GameplayTag : byte
{
    // ... existing tags ...
    
    // Your custom tags
    State_Charmed = 17,
    Buff_Shield = 24,
    Debuff_Bleed = 33,
}
```

## Inspector

- Tags appear as **enum dropdowns**
- Select multiple tags in array fields
- Runtime tags visible in AbilitySystemComponent inspector

## Migration

Old string format → New enum format:
- `"State.Stunned"` → `GameplayTag.State_Stunned`
- `"State.Immune.CC"` → `GameplayTag.State_Immune_CC`
- Dots (`.`) become underscores (`_`)

---

**Full docs**: TAG_SYSTEM_DOCUMENTATION.md
