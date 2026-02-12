# Data-Driven Ability System

## Overview
The GAS (Gameplay Ability System) has been refactored to use a **data-driven architecture** where:
- **Data** = ScriptableObject (designer-editable configuration)
- **Logic** = Singleton behaviour classes (shared, stateless, DI-friendly)
- **Component** = Per-GameObject instance that uses both

This architecture provides:
- ✅ **Memory efficient**: Behaviour logic is shared across all instances
- ✅ **Designer-friendly**: Data is ScriptableObject assets editable in Inspector
- ✅ **Testable**: Logic classes can be unit tested with DI mocking
- ✅ **Scalable**: Easy to add new abilities without modifying core system
- ✅ **Backward compatible**: Old GameplayAbility subclasses still work

---

## Quick Start: Creating a New Ability

### Step 1: Generate Files
1. Open Unity Editor
2. Go to `Tools > GAS > Create New Ability`
3. Enter ability name (e.g., "Fireball")
4. Click "Generate Files"

This creates:
- `FireballAbilityData.cs` - ScriptableObject with configuration
- `FireballAbilityBehaviour.cs` - Logic class with implementation

### Step 2: Register Behaviour in VContainer
Open `FDGameLifetimeScope.cs` and add:
```csharp
builder.Register<FireballAbilityBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
```

### Step 3: Create Data Asset
1. Right-click in Project window
2. Go to `Create > GAS > Abilities > Fireball`
3. Configure properties (damage, cooldown, cost, etc.) in Inspector

### Step 4: Implement Logic
Open `FireballAbilityBehaviour.cs` and implement:
- `CanActivate()` - Custom activation checks
- `OnActivated()` - Ability execution logic
- `OnEnded()` - Cleanup when ability ends
- `OnCancelled()` - Handle interruption/cancellation

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    GameplayAbilityData                      │
│                  (Abstract ScriptableObject)                │
│  - abilityName, cooldown, cost, tags                       │
│  - abstract Type GetBehaviourType()                        │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │ inherits
┌───────────────────────────┴─────────────────────────────────┐
│                    FireballAbilityData                       │
│                    (ScriptableObject)                        │
│  - float damage                                             │
│  - GameObject projectilePrefab                              │
│  - float projectileSpeed                                    │
│  + Type GetBehaviourType() → FireballAbilityBehaviour      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     IAbilityBehaviour                        │
│                       (Interface)                            │
│  + bool CanActivate(data, asc, spec)                        │
│  + void OnActivated(data, asc, spec)                        │
│  + void OnEnded(data, asc, spec)                            │
│  + void OnCancelled(data, asc, spec)                        │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │ implements
┌───────────────────────────┴─────────────────────────────────┐
│                 FireballAbilityBehaviour                     │
│                   (Singleton Logic)                          │
│  - IDebugService debug (injected)                           │
│  + CanActivate() → check prefab assigned                    │
│  + OnActivated() → spawn projectile                         │
│  + OnEnded() → cleanup                                      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│              AbilityBehaviourRegistry                        │
│                 (Singleton Factory)                          │
│  - Dictionary<Type, IAbilityBehaviour> cache               │
│  + IAbilityBehaviour GetBehaviour(data)                     │
│    → Resolves via VContainer + caches                       │
└─────────────────────────────────────────────────────────────┘
```

**Flow:**
1. Designer creates `FireballAbilityData` asset
2. `FireballAbilityData.GetBehaviourType()` returns `typeof(FireballAbilityBehaviour)`
3. `AbilityBehaviourRegistry.GetBehaviour()` resolves behaviour from VContainer
4. Behaviour is cached for subsequent uses (singleton)
5. When ability activates, registry calls `behaviour.OnActivated(data, asc, spec)`

---

## Example: Fireball Ability

### FireballAbilityData.cs
```csharp
[CreateAssetMenu(fileName = "Fireball", menuName = "GAS/Abilities/Fireball")]
public class FireballAbilityData : GameplayAbilityData
{
    [Header("Fireball Settings")]
    public float damage = 50f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    
    public override Type GetBehaviourType()
    {
        return typeof(FireballAbilityBehaviour);
    }
}
```

### FireballAbilityBehaviour.cs
```csharp
public class FireballAbilityBehaviour : IAbilityBehaviour
{
    private readonly IDebugService debug;
    
    public FireballAbilityBehaviour(IDebugService debug)
    {
        this.debug = debug;
    }

    public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
    {
        var fireballData = data as FireballAbilityData;
        return fireballData.projectilePrefab != null;
    }

    public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
    {
        var fireballData = data as FireballAbilityData;
        var owner = asc.GetData().Owner;
        
        // Spawn projectile
        var projectile = Object.Instantiate(
            fireballData.projectilePrefab,
            owner.position + owner.forward,
            Quaternion.LookRotation(owner.forward)
        );
        
        projectile.GetComponent<Rigidbody>().velocity = 
            owner.forward * fireballData.projectileSpeed;
            
        asc.EndAbility(fireballData); // Instant ability
    }
    
    public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
}
```

---

## Dependency Injection in Behaviours

Ability behaviours can inject any services registered in VContainer:

```csharp
public class ComplexAbilityBehaviour : IAbilityBehaviour
{
    private readonly IDebugService debug;
    private readonly IPoolManager poolManager;
    private readonly IEventBus eventBus;
    
    // All dependencies are auto-injected by VContainer
    public ComplexAbilityBehaviour(
        IDebugService debug,
        IPoolManager poolManager,
        IEventBus eventBus)
    {
        this.debug = debug;
        this.poolManager = poolManager;
        this.eventBus = eventBus;
    }
    
    public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
    {
        // Use injected services
        var projectile = poolManager.Spawn("Projectile");
        eventBus.Publish(new AbilityActivatedEvent());
        debug.Log("Ability activated!", Color.cyan);
    }
}
```

---

## Backward Compatibility

Old abilities that inherit `GameplayAbility` still work:

```csharp
// Old style - still works!
public class LegacyFireball : GameplayAbility
{
    public float damage = 50f;
    
    public override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
    {
        // Old implementation
        Debug.Log("Legacy fireball activated!");
    }
}
```

The system uses `LegacyAbilityBehaviour` adapter to route calls to virtual methods.

---

## File Structure

```
Assets/_Master/GAS/Scripts/
├── Base/
│   ├── _GameplayAbility/
│   │   ├── GameplayAbilityData.cs (abstract base)
│   │   ├── IAbilityBehaviour.cs (interface)
│   │   ├── AbilityBehaviourRegistry.cs (factory)
│   │   ├── LegacyAbilityBehaviour.cs (adapter)
│   │   ├── GameplayAbility.cs (legacy support)
│   │   └── Editor/
│   │       └── AbilityCodeGenerator.cs (tool)
│   ├── AbilitySystemData.cs
│   ├── AbilitySystemLogic.cs
│   ├── AbilitySystemComponent.cs
│   ├── GameplayAbilityLogic.cs
│   └── GASInitializer.cs
└── FD/
    └── Abilities/
        ├── FireballAbilityData.cs (example data)
        └── FireballAbilityBehaviour.cs (example logic)
```

---

## VContainer Registration Checklist

For every new ability behaviour, add to `FDGameLifetimeScope.cs`:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    // Core GAS services (already registered)
    builder.Register<AbilitySystemLogic>(Lifetime.Singleton);
    builder.Register<GameplayAbilityLogic>(Lifetime.Singleton);
    builder.Register<AbilityBehaviourRegistry>(Lifetime.Singleton);
    builder.Register<LegacyAbilityBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
    
    // Register your custom ability behaviours here
    builder.Register<FireballAbilityBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
    builder.Register<HealingWaveAbilityBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
    builder.Register<ShieldAbilityBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
    // ... add more as needed
}
```

---

## Best Practices

### ✅ DO:
- Keep behaviour classes **stateless** (singleton)
- Store configuration in Data classes (ScriptableObject)
- Use DI for service dependencies (IDebugService, IPoolManager, etc.)
- Call `asc.EndAbility(data)` when ability completes
- Add custom validation in `CanActivate()`
- Use descriptive names (FireballAbility, not FA or Ability1)

### ❌ DON'T:
- Store instance state in behaviour classes (use AbilitySystemData instead)
- Hardcode values in behaviour logic (use Data properties)
- Skip VContainer registration (behaviour won't be resolved)
- Forget to call `EndAbility()` (ability will stay active)
- Mix old and new patterns in same ability

---

## Testing

Behaviours are easy to unit test:

```csharp
[Test]
public void Fireball_CanActivate_ReturnsFalse_WhenNoPrefab()
{
    // Arrange
    var mockDebug = new Mock<IDebugService>();
    var behaviour = new FireballAbilityBehaviour(mockDebug.Object);
    var data = ScriptableObject.CreateInstance<FireballAbilityData>();
    data.projectilePrefab = null; // No prefab assigned
    
    // Act
    bool canActivate = behaviour.CanActivate(data, null, null);
    
    // Assert
    Assert.IsFalse(canActivate);
}
```

---

## Migration Guide

### Converting Old Ability to New Pattern:

**Before (Old Pattern):**
```csharp
public class OldFireball : GameplayAbility
{
    public float damage = 50f;
    public GameObject projectilePrefab;
    
    public override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
    {
        // Logic here
    }
}
```

**After (New Pattern):**

1. **Create Data class:**
```csharp
[CreateAssetMenu(fileName = "Fireball", menuName = "GAS/Abilities/Fireball")]
public class FireballData : GameplayAbilityData
{
    public float damage = 50f;
    public GameObject projectilePrefab;
    
    public override Type GetBehaviourType() => typeof(FireballBehaviour);
}
```

2. **Create Behaviour class:**
```csharp
public class FireballBehaviour : IAbilityBehaviour
{
    public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
    {
        var fireballData = data as FireballData;
        // Same logic as before
    }
}
```

3. **Register in VContainer:**
```csharp
builder.Register<FireballBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
```

---

## Troubleshooting

### Behaviour Not Found
**Error:** `Type 'FireballAbilityBehaviour' could not be resolved`  
**Fix:** Register behaviour in `FDGameLifetimeScope.cs`

### Null Reference in OnActivated
**Error:** `NullReferenceException: Object reference not set`  
**Fix:** Check if `GetBehaviourType()` returns correct type, and behaviour is registered

### Data Cast Returns Null
**Error:** Cast to specific data type fails  
**Fix:** Ensure Data class and Behaviour class match via `GetBehaviourType()`

### Old Abilities Stop Working
**Error:** Legacy abilities don't activate  
**Fix:** Ensure `LegacyAbilityBehaviour` is registered in VContainer

---

## Summary

The new system separates:
- **What** abilities do (Data in ScriptableObjects)
- **How** they do it (Logic in Singleton behaviours)
- **Where** they run (Component on GameObjects)

This enables:
- Designers to create abilities without code
- Programmers to write testable, reusable logic
- System to scale efficiently with many ability instances

**Next Steps:**
1. Use `Tools > GAS > Create New Ability` to generate files
2. Register behaviour in VContainer
3. Create data asset and configure in Inspector
4. Implement logic in behaviour class
5. Test ability in game!
