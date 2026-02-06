# Fire Area Ability - Hướng Dẫn Sử Dụng

## Mô Tả
Fire Area Ability là một ability tự động bắn lửa vào vị trí enemy mỗi 10 giây một lần. Lửa sẽ kéo dài trong 3 giây và gây 20 damage mỗi giây cho các enemy đứng trong vùng lửa. Enemy bị đốt sẽ nhận được gameplay tag `State.Burning`.

## Các Thành Phần Đã Tạo

### 1. FireAreaAbility.cs
Script chính của ability, bao gồm:
- **FireAreaAbility**: Class chính kế thừa từ `FDGameplayAbility`
  - Cooldown: 10 giây
  - Tự động lấy target từ character và bắn lửa vào vị trí target
  - Tạo vùng lửa tại vị trí đó
  
- **FireAreaDetector**: Component detector cho vùng lửa
  - Phát hiện enemy đi vào/ra khỏi vùng lửa
  - Tự động apply/remove burning effect
  - Tự destroy sau 3 giây

### 2. GameplayEffect - GE_BurningDamage.asset
Gameplay Effect gây damage theo thời gian:
- **Duration Type**: Infinite (cho đến khi enemy rời khỏi vùng lửa)
- **Periodic**: True (mỗi 1 giây)
- **Damage**: -20 Health per second
- **Granted Tag**: `State.Burning`

### 3. Fire Area Prefab - FireAreaPrefab.prefab
GameObject cho vùng lửa:
- Hình dạng: Sphere (Scale: 4x0.5x4 để tạo hình đĩa phẳng)
- Material: FireAreaMaterial (màu cam/đỏ, có emission)
- Components:
  - SphereCollider (trigger)
  - Rigidbody (kinematic)
  - FireAreaDetector (được add runtime)

### 4. Ability Asset - Ability_FireArea.asset
Ability asset đã config sẵn:
- Cooldown: 10 seconds
- Fire Duration: 3 seconds
- Fire Radius: 2 units
- Burning Effect: GE_BurningDamage
- Fire Prefab: FireAreaPrefab

## Cách Sử Dụng

### Setup Cho Tower
1. Mở Tower prefab hoặc Tower trong scene
2. Tìm component `TowerBase`
3. Trong phần **Abilities**:
   - Add new ability entry
   - Assign `Ability_FireArea.asset` vào ability slot
   - Set level (khuyến nghị: 1)
   - **Không** tick `isPassive` (ability sẽ tự động activate mỗi khi off cooldown)

### Passive vs Non-Passive
- **Passive = false** (khuyến nghị): Ability sẽ được TowerBase tự động kích hoạt khi có target và off cooldown
- **Passive = true**: Ability sẽ được kích hoạt ngay khi tower spawn (không phù hợp với ability này)

### Test Trong Scene
1. Tạo một tower với FireAreaAbility
2. Spawn enemy trong range của tower
3. Sau 10 giây (hoặc ngay lập tức nếu cooldown = 0), lửa sẽ xuất hiện tại vị trí enemy
4. Lửa kéo dài 3 giây
5. Enemy đứng trong lửa sẽ nhận 20 damage/giây
6. Sau 3 giây, lửa tự động biến mất

## Customize

### Thay Đổi Damage
Mở `GE_BurningDamage.asset`:
- Sửa Modifier > Scalable Magnitude từ -20 thành giá trị mong muốn
- Ví dụ: -30 = 30 damage/giây

### Thay Đổi Fire Duration
Mở `Ability_FireArea.asset`:
- Sửa `Fire Duration` từ 3 thành giá trị mong muốn (seconds)

### Thay Đổi Cooldown
Mở `Ability_FireArea.asset`:
- Sửa `Cooldown Duration` từ 10 thành giá trị mong muốn (seconds)

### Thay Đổi Fire Radius
Mở `Ability_FireArea.asset`:
- Sửa `Fire Radius` từ 2 thành giá trị mong muốn (units)

### Thay Đổi Visual
1. Tạo material mới hoặc VFX particle system
2. Tạo prefab mới với visual đó
3. Assign prefab mới vào `Ability_FireArea.asset` > `Fire Area Prefab`

## Debug

### Enable Debug Logs
Mở `Ability_FireArea.asset`:
- Tick `Show Debug` để xem logs trong Console

### Common Issues

**Lửa không xuất hiện:**
- Check console xem có log "[FireAreaAbility] No targets found"
- Đảm bảo tower có target trong range
- Đảm bảo ability off cooldown

**Enemy không bị damage:**
- Check console xem có log "Applied burning effect to..."
- Đảm bảo enemy có `AbilitySystemComponent`
- Đảm bảo enemy có `BaseCharacter` component
- Check enemy layer mask có match với `Enemy Layer Mask` trong ability

**Lửa không biến mất:**
- Check fire duration có được set đúng
- Đảm bảo FireAreaDetector component đang hoạt động

## Gameplay Tags

Ability này sử dụng tag:
- `State.Burning`: Được grant cho enemy đang đứng trong lửa
- Có thể dùng tag này để:
  - Tạo visual effect cho enemy đang cháy
  - Check immunity (enemy có thể có `State.Immune` hoặc `State.Immune_Fire`)
  - Tạo combo với abilities khác (ví dụ: ability gây thêm damage cho enemy đang bị Burning)

## Mở Rộng

### Thêm Slow Effect
Để thêm slow effect khi enemy đứng trong lửa:
1. Mở `GE_BurningDamage.asset`
2. Thêm modifier mới:
   - Attribute: MoveSpeed
   - Operation: Multiply
   - Magnitude: 0.5 (giảm 50% tốc độ)

### Thêm Visual Effect Cho Enemy
Tạo một script mới:
```csharp
public class BurningVisualEffect : MonoBehaviour
{
    [SerializeField] private GameObject fireVFX;
    private AbilitySystemComponent asc;
    
    void Start()
    {
        asc = GetComponent<AbilitySystemComponent>();
        if (asc != null)
        {
            asc.OnTagAdded += OnTagChanged;
            asc.OnTagRemoved += OnTagChanged;
        }
    }
    
    private void OnTagChanged(GameplayTag tag)
    {
        if (tag == GameplayTag.State_Burning)
        {
            bool isBurning = asc.HasTag(GameplayTag.State_Burning);
            fireVFX.SetActive(isBurning);
        }
    }
}
```

## Files Created
- `/Assets/_Master/Scripts/Abilities/FireAreaAbility.cs` - Main ability script
- `/Assets/_Master/Scripts/Editor/BurningEffectCreator.cs` - Editor utility
- `/Assets/_Master/Scripts/Editor/FireAreaAbilityCreator.cs` - Editor utility
- `/Assets/Prefabs/Abilities/GE_BurningDamage.asset` - GameplayEffect asset
- `/Assets/Prefabs/Abilities/FireAreaMaterial.mat` - Fire material
- `/Assets/Prefabs/Abilities/FireAreaPrefab.prefab` - Fire area prefab
- `/Assets/Prefabs/Abilities/Ability_FireArea.asset` - Ability asset
