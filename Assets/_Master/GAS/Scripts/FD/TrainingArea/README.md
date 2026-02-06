# Battle Training System

A Dota 2-inspired battle training system for testing abilities and game mechanics.

## Features

### 1. Training Player
- **TrainingPlayer.cs**: A specialized player character for testing abilities
- Dynamic ability selection through inspector
- Auto-targeting system with configurable range
- Visual gizmos for target range and current target

### 2. Dummy Enemy
- **DummyEnemy.cs**: Training dummy with customizable properties
- Auto-regeneration (configurable HP/s)
- Invulnerability toggle
- Full Ability System Component (ASC) integration
- Health reset functionality

### 3. Custom Inspector
- **TrainingPlayerEditor.cs**: Enhanced inspector for TrainingPlayer
- Dropdown ability selection
- Shows selected ability details inline
- Runtime buttons for testing:
  - Activate Selected Ability
  - Find Nearest Target
  - Reset Stats

### 4. Battle Training UI
- **BattleTrainingUI.cs**: Comprehensive training interface similar to Dota 2
- Spawn controls for enemies and allies
- Ability activation button
- Player stats display (HP/MP bars)
- Enemy settings:
  - Invulnerability toggle
  - Auto-regen toggle
  - Regen rate slider
- Utility buttons:
  - Find Target
  - Reset Player
  - Clear All

## How to Use

### Setting Up the Training Scene

1. **Open the Scene**: Load `Assets/Scenes/BattleTraining.unity`

2. **Configure Training Player**:
   - Select the "TrainingPlayer" GameObject
   - In the inspector, add your gameplay abilities to "Available Abilities"
   - Set "Auto Target Range" (default: 10 units)
   - Configure "Target Layer" to match enemy layer
   - Select which ability to test from the dropdown

3. **Create Prefabs**:
   - **Dummy Enemy Prefab**:
     - Create a GameObject with a mesh (Capsule, Cube, etc.)
     - Add `AbilitySystemComponent`
     - Add `DummyEnemy` script
     - Create an initial GameplayEffect with starting health values
     - Assign to BattleTrainingUI's "Dummy Enemy Prefab" field
   
   - **Ally Prefab** (optional):
     - Similar setup to Dummy Enemy
     - Assign to BattleTrainingUI's "Ally Prefab" field

4. **Configure UI**:
   - Select the "UI" GameObject
   - In BattleTrainingUI component:
     - Assign "Training Player" reference
     - Assign "Spawn Point" (or leave empty for spawn near player)
     - Assign prefabs
     - Wire up all UI element references (buttons, sliders, text)

### Testing Abilities

#### Method 1: Custom Inspector
1. Select TrainingPlayer GameObject
2. Choose ability from dropdown in inspector
3. Click "Activate Selected Ability" button (Play mode only)

#### Method 2: UI System
1. Enter Play mode
2. Click "Create Enemy" to spawn a dummy
3. Click "Find Target" to auto-select nearest target
4. Click "Activate Ability" to test your ability

### Inspector Features

The TrainingPlayer custom inspector shows:
- **Available Abilities List**: Drag abilities here
- **Selected Ability Dropdown**: Choose which ability to test
- **Selected Ability Details**: View all properties of the selected ability
- **Target Settings**: Configure auto-targeting
- **Runtime Controls**: Test buttons (Play mode only)

### Training Controls

**Spawn Controls:**
- **Create Enemy**: Spawn a dummy enemy at spawn point
- **Create Ally**: Spawn an allied unit

**Ability Testing:**
- **Activate Ability**: Trigger the currently selected ability
- **Find Target**: Auto-select nearest enemy

**Utility:**
- **Reset Player**: Restore HP/MP to maximum
- **Clear All**: Remove all spawned entities

**Enemy Settings:**
- **Invulnerable**: Toggle damage immunity
- **Auto Regen**: Toggle automatic health regeneration
- **Regen Rate**: Adjust HP regeneration per second

## Code Architecture

### TrainingPlayer
```csharp
public class TrainingPlayer : MonoBehaviour, IAbilitySystemComponent
{
    // Select and manage abilities
    public void SelectAbility(int index)
    public void AddAbility(GameplayAbility ability)
    public void ActivateSelectedAbility()
    
    // Targeting
    public void SetTarget(Transform target)
    public void FindNearestTarget()
    public List<Transform> GetTargets()
    
    // Utility
    public void ResetStats()
}
```

### DummyEnemy
```csharp
public class DummyEnemy : MonoBehaviour, IAbilitySystemComponent
{
    // Settings
    public void SetInvulnerable(bool value)
    public void SetAutoRegen(bool value)
    public void SetRegenRate(float rate)
    
    // Utility
    public void ResetHealth()
}
```

### BattleTrainingUI
```csharp
public class BattleTrainingUI : MonoBehaviour
{
    // Spawn management
    private void OnCreateEnemy()
    private void OnCreateAlly()
    private void OnClearAll()
    
    // Ability control
    private void OnActivateAbility()
    private void OnAbilitySelected(int index)
    
    // Player management
    private void OnResetPlayer()
    private void OnFindTarget()
    
    // Stats display
    private void UpdatePlayerStats()
}
```

## Tips

1. **Ability Selection**: The dropdown in the inspector dynamically shows all abilities in the Available Abilities list

2. **Targeting**: Use the auto-targeting system for quick testing. The yellow gizmo shows the target range

3. **Enemy Settings**: Toggle invulnerability when you want to test ability visuals without killing the dummy

4. **Regen Rate**: Set high regen (100+ HP/s) to keep dummy alive indefinitely during extended testing

5. **Multiple Enemies**: Use "Create Enemy" multiple times to test AOE abilities

6. **Stats Monitoring**: The UI automatically updates player HP/MP in real-time

## Extending the System

### Adding New Controls
Add new buttons to the UI and create handler methods in `BattleTrainingUI.cs`:

```csharp
private void OnMyNewButton()
{
    // Your code here
}
```

### Custom Enemy Types
Create new enemy scripts that inherit from `DummyEnemy` or implement `IAbilitySystemComponent`:

```csharp
public class AdvancedDummy : DummyEnemy
{
    // Add custom behavior
}
```

### Ability-Specific UI
Extend `TrainingPlayerEditor` to show custom inspector elements for specific ability types:

```csharp
if (selectedAbility is MyCustomAbility customAbility)
{
    // Show custom UI
}
```

## Known Limitations

1. UI references must be manually wired in the inspector
2. Ability dropdown doesn't auto-refresh when abilities list changes in Play mode
3. Spawned entities are tracked by list, not by tags/layers

## Future Enhancements

- [ ] Save/Load training configurations
- [ ] Ability cooldown display
- [ ] Damage log/combat text
- [ ] Advanced enemy AI behaviors
- [ ] Prefab spawning presets
- [ ] Ability combo recorder
- [ ] Performance metrics (DPS calculator)
