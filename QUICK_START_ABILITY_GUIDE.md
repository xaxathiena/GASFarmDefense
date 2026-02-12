# Quick Start: Creating Your First Ability

This guide will walk you through creating a simple "Lightning Strike" ability using the new data-driven system.

## Step 1: Generate Ability Files

1. In Unity Editor, go to **Tools > GAS > Create New Ability**
2. Enter ability name: `LightningStrike`
3. Click **Generate Files**

This creates:
- `LightningStrikeData.cs`
- `LightningStrikeBehaviour.cs`

## Step 2: Configure the Data Class

Open `LightningStrikeData.cs` and add your custom properties:

```csharp
using UnityEngine;
using GAS;

namespace FD.Abilities
{
    [CreateAssetMenu(fileName = "LightningStrike", menuName = "GAS/Abilities/LightningStrike")]
    public class LightningStrikeData : GameplayAbilityData
    {
        [Header("Lightning Strike Settings")]
        public float damage = 100f;
        public float areaRadius = 5f;
        public GameObject strikeVFX;
        public LayerMask targetLayers;

        public override System.Type GetBehaviourType()
        {
            return typeof(LightningStrikeBehaviour);
        }
    }
}
```

## Step 3: Implement the Behaviour Logic

Open `LightningStrikeBehaviour.cs` and implement the ability:

```csharp
using UnityEngine;
using GAS;

namespace FD.Abilities
{
    public class LightningStrikeBehaviour : IAbilityBehaviour
    {
        private readonly IDebugService debug;
        
        public LightningStrikeBehaviour(IDebugService debug)
        {
            this.debug = debug;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var lightningData = data as LightningStrikeData;
            
            // Check if VFX is assigned
            if (lightningData.strikeVFX == null)
            {
                debug.Log("Lightning Strike: No VFX prefab assigned!", Color.red);
                return false;
            }
            
            return true;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var lightningData = data as LightningStrikeData;
            if (lightningData == null) return;

            var owner = asc.GetData().Owner;
            
            debug.Log($"Lightning Strike! Damage: {lightningData.damage}, Radius: {lightningData.areaRadius}", Color.cyan);

            // Spawn VFX at owner position
            if (lightningData.strikeVFX != null)
            {
                var vfx = Object.Instantiate(lightningData.strikeVFX, owner.position, Quaternion.identity);
                Object.Destroy(vfx, 2f); // Cleanup after 2 seconds
            }

            // Find enemies in radius and damage them
            var colliders = Physics.OverlapSphere(owner.position, lightningData.areaRadius, lightningData.targetLayers);
            foreach (var col in colliders)
            {
                var enemyASC = col.GetComponent<AbilitySystemComponent>();
                if (enemyASC != null && enemyASC != asc) // Don't damage self
                {
                    // Apply damage (you can create a DamageGameplayEffect for this)
                    var attributeSet = enemyASC.GetAttributeSet<AttributeSet>();
                    if (attributeSet != null)
                    {
                        // For now, we'll just log it
                        debug.Log($"Hit {col.name} for {lightningData.damage} damage!", Color.yellow);
                        
                        // In production, you'd apply a damage effect:
                        // var damageEffect = ScriptableObject.CreateInstance<DamageGameplayEffect>();
                        // damageEffect.damageAmount = lightningData.damage;
                        // asc.ApplyGameplayEffectToTarget(damageEffect, enemyASC, asc, spec.Level);
                    }
                }
            }

            // End ability immediately (it's an instant cast)
            asc.EndAbility(lightningData);
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            debug.Log("Lightning Strike ended", Color.gray);
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            debug.Log("Lightning Strike cancelled", Color.yellow);
        }
    }
}
```

## Step 4: Register in VContainer

Open `FDGameLifetimeScope.cs` and add the behaviour registration:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    // ... existing registrations ...
    
    // Ability Behaviours
    builder.Register<FireballAbilityBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>();
    builder.Register<LightningStrikeBehaviour>(Lifetime.Singleton).As<IAbilityBehaviour>(); // Add this line
}
```

## Step 5: Create the Data Asset

1. In Unity Project window, right-click in `Assets/_Master/GAS/Scripts/FD/Abilities/`
2. Go to **Create > GAS > Abilities > LightningStrike**
3. Name it `LightningStrike_Basic`
4. Configure the properties in Inspector:
   - Ability Name: "Lightning Strike"
   - Cooldown Duration: 5 seconds
   - Cost Amount: 30 mana
   - Damage: 100
   - Area Radius: 5
   - Strike VFX: (assign your lightning VFX prefab)
   - Target Layers: (select enemy layers)

## Step 6: Grant Ability to Character

In your tower/character initialization code:

```csharp
public class TowerController : ITickable
{
    private readonly AbilitySystemComponent acs;
    private TowerData towerData;
    
    // In OnSetup or Start method:
    public void OnSetup(TowerView view, TowerData data)
    {
        this.towerData = data;
        
        // Grant Lightning Strike ability
        var lightningStrikeAbility = Resources.Load<LightningStrikeData>("Abilities/LightningStrike_Basic");
        if (lightningStrikeAbility != null)
        {
            acs.GiveAbility(lightningStrikeAbility, level: 1);
        }
    }
    
    // Activate ability (e.g., in Update or on input)
    public void OnAttackInput()
    {
        var lightningStrikeAbility = Resources.Load<LightningStrikeData>("Abilities/LightningStrike_Basic");
        if (lightningStrikeAbility != null)
        {
            acs.TryActivateAbility(lightningStrikeAbility);
        }
    }
}
```

## Step 7: Test!

1. Play the scene
2. Trigger the ability (via input or AI)
3. Check Console for debug logs
4. Verify VFX spawns and enemies are hit

---

## Advanced: Creating a Channeled Ability

For abilities that run over time (like a laser beam):

```csharp
public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
{
    var channelData = data as ChannelAbilityData;
    
    // Start channel effect (VFX, animation, etc.)
    var beam = Object.Instantiate(channelData.beamPrefab, owner.position, owner.rotation);
    
    // Apply continuous effect using GameplayEffect
    var dotEffect = ScriptableObject.CreateInstance<PeriodicDamageEffect>();
    dotEffect.damagePerTick = channelData.damagePerSecond;
    dotEffect.tickInterval = 0.5f;
    dotEffect.duration = channelData.channelDuration;
    
    asc.ApplyGameplayEffectToTarget(dotEffect, targetASC, asc, spec.Level);
    
    // Schedule end ability after channel duration
    // (You can use a coroutine or timer system here)
    
    // DON'T call asc.EndAbility() immediately for channeled abilities!
}

public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
{
    // Clean up channel effect if interrupted
    // Stop VFX, remove DoT, etc.
}
```

---

## Tips

### Performance
- Behaviour classes are **Singletons** - they're shared across all ability instances
- Keep behaviours **stateless** - store state in `AbilitySystemData` or `GameplayAbilitySpec`
- Use **object pooling** for frequently spawned objects (projectiles, VFX)

### Debugging
- Use `IDebugService` for colored console logs
- Check `AbilitySystemComponent.GetAbilitySpec(ability)` to inspect ability state
- Use `acs.GetAbilityCooldownRemaining(ability)` to debug cooldowns

### Organization
- Store ability data assets in: `Assets/_Master/GAS/Data/Abilities/`
- Group similar abilities: `FireMagic/`, `IceMagic/`, `Physical/`
- Use consistent naming: `[AbilityName]Data.asset`, `[AbilityName]Behaviour.cs`

### Common Patterns

**Cost Check:**
```csharp
public bool CanActivate(...)
{
    var manaAttribute = asc.GetAttributeSet<ManaAttributeSet>();
    return manaAttribute.CurrentMana >= myAbilityData.manaCost;
}
```

**Target Validation:**
```csharp
public bool CanActivate(...)
{
    var target = GetTargetFromSpec(spec); // Your target selection logic
    if (target == null) return false;
    
    float distance = Vector3.Distance(owner.position, target.position);
    return distance <= myAbilityData.range;
}
```

**Combo System:**
```csharp
public bool CanActivate(...)
{
    // Require "Combo" tag from previous ability
    return asc.HasAnyTags(GameplayTag.Combo);
}
```

---

## Next Steps

1. Create more abilities using the code generator
2. Implement a **DamageGameplayEffect** for standardized damage
3. Create **buff/debuff effects** (speed boost, slow, stun)
4. Build an **ability bar UI** that displays cooldowns
5. Add **ability upgrades** (increase spec.Level, scale damage)

Happy coding! 🎮⚡
