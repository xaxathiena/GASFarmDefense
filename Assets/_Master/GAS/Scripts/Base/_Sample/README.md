# Character AI Sample

Simple character AI system using FSM (Finite State Machine) with GAS abilities.

## Components

### 1. **CharacterAI.cs** - Main AI Controller
FSM-based AI that controls character behavior:
- **Idle**: Wait between actions
- **SearchTarget**: Look for enemies
- **NormalAttack**: Attack nearest enemy
- **UseSkill**: Use heal skill when health < 50%
- **Dead**: Character died

### 2. **CharacterStates.cs** - FSM State Classes
Individual state implementations for the FSM.

### 3. **NormalAttackAbility.cs** - Normal Attack
- Finds nearest enemy in range
- Deals damage
- Instant cast

### 4. **HealSkillAbility.cs** - Heal Skill
- Heals self for 10 HP
- 10 second cooldown
- Used when health < 50%

## Setup Instructions

### Step 1: Create Character GameObject
1. Create Empty GameObject named "Character"
2. Add components:
   - `AbilitySystemComponent`
   - `CharacterAI`
3. Create or assign an AttributeSet (ExampleAttributeSet)

### Step 2: Create Abilities
1. **Normal Attack**:
   - Right-click → Create → GAS → Sample → Normal Attack Ability
   - Set damage, attack range, enemy layers
   
2. **Heal Skill**:
   - Right-click → Create → GAS → Sample → Heal Skill Ability
   - Set heal amount (10)
   - Set cooldown in ability base settings (10s)

### Step 3: Create Gameplay Effects (Optional)
1. **Damage Effect**:
   - Create → GAS → Effects → Damage Effect
   - Set Health modifier to -10 (or desired damage)
   
2. **Heal Effect**:
   - Create → GAS → Effects → Heal Effect
   - Set Health modifier to +10

### Step 4: Configure CharacterAI
In the Character GameObject's CharacterAI component:
- Assign Normal Attack Ability
- Assign Heal Skill Ability
- Set Detection Range (e.g., 10)
- Set Action Cooldown (e.g., 1)
- Set Enemy Layers

### Step 5: Configure Abilities
In the Heal Skill Ability asset:
- Set `cooldownDuration` to 10

In the Normal Attack Ability asset:
- Set damage amount
- Set attack range
- Assign damage effect (optional)

## AI Behavior Logic

```
Update Loop:
├─ Idle State
│  └─ Wait for action cooldown
│     └─ Go to Search Target
│
├─ Search Target State
│  ├─ Look for enemies in range
│  │  ├─ Found enemies
│  │  │  └─ Decision Logic:
│  │  │     ├─ Health < 50% AND Heal available?
│  │  │     │  └─ Use Heal Skill
│  │  │     └─ Else
│  │  │        └─ Normal Attack
│  │  └─ No enemies
│  │     └─ Back to Idle
│
├─ Normal Attack State
│  ├─ Find nearest enemy
│  ├─ Deal damage
│  └─ Back to Idle
│
├─ Use Skill State
│  ├─ Heal self
│  ├─ Start 10s cooldown
│  └─ Back to Idle
│
└─ Dead State
   └─ Disable AI
```

## Testing

1. Create 2 Character GameObjects
2. Assign different layers (e.g., "Player" and "Enemy")
3. Set each character's enemy layer to target the other
4. Press Play
5. Watch characters:
   - Search for each other
   - Attack when in range
   - Heal when health is low
   - Die when health reaches 0

## Customization

### Change AI Logic
Edit `SearchTargetState.DecideAction()` in CharacterStates.cs:
```csharp
private void DecideAction()
{
    float healthPercent = health.GetPercentage();
    bool canUseSkill = !asc.IsAbilityOnCooldown(healSkill);
    
    // Your custom logic here
    if (healthPercent < 0.3f && canUseSkill) // Changed threshold
    {
        UseSkill();
    }
    // Add more conditions...
}
```

### Add New States
1. Create new state class in CharacterStates.cs
2. Add to ECharacterState enum
3. Register in CharacterAI.Awake()
4. Add transition logic in existing states

### Add More Abilities
1. Create new ability ScriptableObject
2. Add field in CharacterAI
3. Grant ability in CharacterAI.Start()
4. Add decision logic in SearchTargetState

## Debug Features

- **Gizmos**: Shows detection range (yellow) and attack range (red)
- **Console Logs**: All state transitions and actions are logged
- **Health Display**: Health changes are logged

## Example Values

```
Character Health: 100
Normal Attack Damage: 10
Heal Amount: 10
Heal Cooldown: 10s
Detection Range: 10 units
Attack Range: 3 units
Action Cooldown: 1s
```

## Notes

- Characters will prioritize healing when health < 50%
- Normal attack targets nearest enemy only
- AI runs independently for each character
- FSM pattern makes it easy to add new behaviors
