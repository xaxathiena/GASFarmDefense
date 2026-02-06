# Battle Training - Quick Setup Guide

## What Was Created

### Scripts
1. **DummyEnemy.cs** - Training dummy with ASC, auto-regen, invulnerability
2. **TrainingPlayer.cs** - Player with ability selection and targeting
3. **TrainingPlayerEditor.cs** - Custom inspector showing ability details
4. **BattleTrainingUI.cs** - Dota 2-style UI manager
5. **UILayoutHelper.cs** - Helper to auto-arrange UI buttons

### Scene
- **BattleTraining.unity** - Pre-configured training scene with:
  - Main Camera and Directional Light
  - Ground plane
  - TrainingPlayer with ASC and TrainingPlayer component
  - UI Canvas with BattleTrainingUI component
  - Event System for UI interaction
  - UI Panels and Buttons (ready to wire up)

## Next Steps to Complete Setup

### 1. Create Dummy Enemy Prefab
```
1. Create > 3D Object > Capsule
2. Name it "DummyEnemy"
3. Add Component > AbilitySystemComponent
4. Add Component > DummyEnemy
5. Create a GameplayEffect asset with initial HP values
6. Assign to DummyEnemy's "Initial Effect" field
7. Drag to Project to make it a prefab
```

### 2. Wire Up the UI
```
1. Open BattleTraining scene
2. Select UI GameObject
3. In BattleTrainingUI component, assign:
   - Training Player: TrainingPlayer GameObject
   - Spawn Point: SpawnPoint GameObject
   - Dummy Enemy Prefab: Your DummyEnemy prefab
   - All button references (ActivateAbilityBtn, CreateEnemyBtn, etc.)
   - All UI element references (sliders, texts, toggles)
```

### 3. Add Abilities to Test
```
1. Select TrainingPlayer GameObject
2. In inspector, find "Available Abilities" list
3. Drag your GameplayAbility assets into the list
4. The dropdown will show all abilities
5. In Play mode, select and test abilities!
```

### 4. Optional: Use UI Layout Helper
```
1. Select MainPanel GameObject
2. Add Component > UILayoutHelper
3. In inspector, click "Layout Children" to auto-arrange buttons
4. Click "Add Text to Buttons" to add text labels
5. Click "Setup Panel Background" to style the panel
6. Adjust spacing, button size, columns as needed
```

## Testing Your Setup

1. Enter Play Mode
2. Click "Create Enemy" - should spawn a dummy at SpawnPoint
3. Select an ability from TrainingPlayer inspector dropdown
4. Click "Activate Ability" button in UI or inspector
5. Watch your ability execute!

## Inspector Features

### TrainingPlayer Inspector
When you select TrainingPlayer in the scene, you'll see:
- **Ability System Component**: Reference to ASC
- **Initial Effect**: Starting stats effect
- **Available Abilities**: List of abilities to test
- **Selected Ability**: Dropdown to choose active ability
- **Selected Ability Details**: Full inspector of chosen ability
- **Target Settings**: Auto-targeting configuration
- **Runtime Buttons** (Play mode):
  - Activate Selected Ability
  - Find Nearest Target
  - Reset Stats

### Dynamic Inspector
The inspector automatically shows the selected ability's properties inline, so you can see all ability settings without leaving the TrainingPlayer inspector!

## Customization

### Adjust Dummy Settings
- Auto Regen: Toggle in UI or set default in prefab
- Regen Rate: Slider in UI (0-100 HP/s recommended)
- Invulnerable: Toggle in UI for testing visuals

### Modify Spawn Behavior
Edit `BattleTrainingUI.cs`:
- `spawnRadius`: How far from spawn point to randomize
- Spawn position logic in `GetSpawnPosition()`

### Add Custom Controls
1. Add new buttons to UI
2. Create handler methods in `BattleTrainingUI.cs`
3. Wire up in inspector

## Troubleshooting

**Ability doesn't activate:**
- Check Mana cost vs current Mana
- Check cooldown hasn't expired
- Verify target is assigned
- Check ability CanActivate conditions

**Can't find target:**
- Set Target Layer in TrainingPlayer
- Make sure enemy is within Auto Target Range
- Check enemy has collider

**UI doesn't respond:**
- Verify EventSystem exists in scene
- Check button references in BattleTrainingUI
- Make sure Canvas is in ScreenSpaceOverlay mode

**Dummy doesn't take damage:**
- Check if Invulnerable is toggled
- Verify ability has GameplayEffect with damage
- Check AttributeSet initialization

## Tips

- Use Scene view gizmos to visualize target range (yellow wire sphere)
- Red line shows current target in Scene view
- Toggle auto-regen OFF for precise damage testing
- Set regen to 100+ for immortal dummy
- Use "Clear All" to reset training area quickly
