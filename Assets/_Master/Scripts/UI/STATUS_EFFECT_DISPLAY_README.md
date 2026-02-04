# Status Effect Display System

Hệ thống hiển thị các hiệu ứng trạng thái (status effects) trên đầu nhân vật/enemy theo thời gian thực.

## Thành Phần

### 1. StatusEffectIcon
Component UI để hiển thị một icon effect với:
- Icon sprite
- Fill image (progress bar tròn)
- Stack count text
- Timer text
- Fade in/out animations
- Pulse animation khi stack tăng

### 2. StatusEffectDisplayManager
Manager quản lý việc hiển thị các icons:
- Theo dõi AbilitySystemComponent
- Tự động thêm/xóa icons khi effects thay đổi
- Cập nhật vị trí theo nhân vật (world space → screen space)
- Layout icons theo hàng ngang

### 3. StatusEffectIconDatabase
ScriptableObject database để map GameplayTag → Icon Sprite:
- Dễ dàng gán icon cho từng tag
- Context menu "Add All Tags" để thêm tất cả tags
- Cache system cho performance

### 4. StatusEffectDisplayHandler
Helper component tự động setup display cho character:
- Attach vào GameObject có AbilitySystemComponent
- Tự động tạo canvas container
- Tự động khởi tạo display manager

## Cài Đặt

### Bước 1: Tạo Icon Database
1. Right-click trong Project → `Create > FD > UI > Status Effect Icon Database`
2. Name: `StatusEffectIconDatabase`
3. Right-click asset → `Add All Tags` để thêm tất cả GameplayTags
4. Gán icon sprites cho từng tag (Slow, Stun, Burn, etc.)

### Bước 2: Tạo Status Effect Icon Prefab
1. Tạo GameObject → `StatusEffectIcon`
2. Add component: `StatusEffectIcon`
3. Setup UI:
   ```
   StatusEffectIcon (CanvasGroup)
   ├─ IconImage (Image) - Background/frame
   ├─ FillImage (Image) - Radial fill, fill amount = 1
   ├─ StackText (TextMeshProUGUI) - Top right corner
   └─ TimerText (TextMeshProUGUI) - Center
   ```
4. Gán references trong inspector
5. Save as prefab

### Bước 3: Setup Canvas
1. Tạo hoặc sử dụng Canvas có sẵn
2. Add child: `StatusEffectContainer` (RectTransform)
3. Optional: Add HorizontalLayoutGroup cho auto layout

### Bước 4: Add vào Characters
Có 2 cách:

#### Cách 1: Tự động (Recommended)
```csharp
// Chỉ cần add component vào character
gameObject.AddComponent<StatusEffectDisplayHandler>();
```

Component sẽ tự động:
- Tìm Canvas
- Tạo container
- Setup display manager
- Initialize với ASC

#### Cách 2: Manual
```csharp
// Trong character script
using FD.UI;

[SerializeField] private StatusEffectDisplayManager displayManager;

void Start()
{
    var asc = GetComponent<AbilitySystemComponent>();
    displayManager.Initialize(asc);
}
```

## Sử Dụng

### Setup cho Enemy
```csharp
// FDEnemy.cs
using FD.Character;

public class FDEnemy : MonoBehaviour
{
    private void Awake()
    {
        // Add status effect display
        gameObject.AddComponent<StatusEffectDisplayHandler>();
    }
}
```

### Setup cho Tower
```csharp
// TowerBase.cs
public class TowerBase : MonoBehaviour
{
    [SerializeField] private StatusEffectDisplayHandler effectDisplay;
    
    private void Start()
    {
        if (effectDisplay == null)
        {
            effectDisplay = gameObject.AddComponent<StatusEffectDisplayHandler>();
        }
    }
}
```

### Custom Layout
```csharp
// Thay đổi vị trí hiển thị
[SerializeField] private Vector3 worldOffset = new Vector3(0, 3f, 0); // Cao hơn

// Thay đổi số lượng icons tối đa
[SerializeField] private int maxVisibleIcons = 8;

// Thay đổi khoảng cách giữa icons
[SerializeField] private float iconSpacing = 50f;
```

## Tính Năng

### Hiển Thị Thông Tin
- **Icon**: Visual representation của effect
- **Timer**: Thời gian còn lại (hoặc ∞ cho infinite)
- **Fill**: Progress bar tròn
- **Stacks**: Số lượng stack (nếu > 1)

### Animation
- **Fade In**: Khi effect được apply
- **Fade Out**: Khi effect hết hạn
- **Pulse**: Khi stack tăng
- **Smooth updates**: Timer và fill bar mượt mà

### Auto Update
- Tự động theo dõi active effects
- Tự động thêm icon khi effect mới xuất hiện
- Tự động xóa icon khi effect hết
- Tự động update timer và fill amount

### World Space Tracking
- Icons luôn ở trên đầu character
- Tự động ẩn khi character ở ngoài camera
- Hỗ trợ cả Screen Space Overlay và Camera canvas

## Example: Slow Effect

```csharp
// 1. Tạo slow effect asset
[CreateAssetMenu]
public class SlowEffect : GameplayEffect
{
    void OnValidate()
    {
        grantedTags = new[] { GameplayTag.Debuff_Slow };
        durationType = EGameplayEffectDurationType.Duration;
        durationMagnitude = 5f; // 5 seconds
        
        modifiers = new[] {
            new GameplayEffectModifier {
                attribute = EGameplayAttributeType.MoveSpeed,
                operation = EGameplayModifierOp.Multiply,
                magnitude = 0.5f // 50% slow
            }
        };
    }
}

// 2. Gán slow icon trong StatusEffectIconDatabase
// Tag: Debuff_Slow → Icon: slow_icon_sprite

// 3. Apply effect
asc.ApplyGameplayEffectToSelf(slowEffect);

// 4. Icon sẽ tự động xuất hiện với:
// - Slow icon
// - Timer đếm ngược 5s
// - Fill bar giảm dần
// - Tự động biến mất khi hết thời gian
```

## Tips & Best Practices

### Performance
- Chỉ show effects có `grantedTags` (visible status effects)
- Giới hạn số icons tối đa (maxVisibleIcons)
- Icons tự động pool khi fade out

### Visual Design
- Icon size recommend: 32x32 hoặc 64x64
- Sử dụng màu sắc rõ ràng cho mỗi effect type:
  - Slow: Blue
  - Stun: Yellow
  - Burn: Red/Orange
  - Poison: Green/Purple
  - Buff: Gold/White

### Layout
- Horizontal layout cho nhiều effects
- Max 6-8 icons để tránh clutter
- Spacing 5-10px giữa icons

### Integration với GAS
```csharp
// Subscribe to effect events (nếu cần custom logic)
asc.OnGameplayEffectApplied += OnEffectApplied;
asc.OnGameplayEffectRemoved += OnEffectRemoved;

void OnEffectApplied(ActiveGameplayEffect effect)
{
    // Custom logic khi effect được apply
    // Display manager sẽ tự động handle UI
}
```

## Troubleshooting

### Icons không hiện
- Check StatusEffectIconDatabase có gán icons chưa
- Check effect có `grantedTags` chưa
- Check Canvas có active không

### Vị trí sai
- Điều chỉnh `worldOffset` trong StatusEffectDisplayHandler
- Check camera reference

### Performance issues
- Giảm `maxVisibleIcons`
- Tối ưu icon sprites (compress)
- Disable effects cho objects xa camera

## TODO / Future Enhancements
- [ ] Tooltip khi hover icon
- [ ] Grouping effects theo category
- [ ] Priority system (show important effects first)
- [ ] Sound effects khi apply/remove
- [ ] Particle effects integration
- [ ] Mobile-friendly touch tooltips

---

**Tham khảo**: System này được thiết kế dựa trên DamagePopupManager pattern, sử dụng DOTween cho animations và tích hợp với Gameplay Ability System.
