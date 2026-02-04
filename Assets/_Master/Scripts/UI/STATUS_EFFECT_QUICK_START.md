# Status Effect Display - Quick Start Guide

## Các Đối Tượng Đã Tạo Trong Unity

### 1. StatusEffectIcon GameObject
✅ **Đã tạo trong scene** với cấu trúc:
```
StatusEffectIcon
├─ IconBackground (Image - background/frame)
├─ FillImage (Image - radial fill progress)
├─ StackText (TextMeshProUGUI - stack count)
└─ TimerText (TextMeshProUGUI - timer)
```

**Vị trí**: Scene hierarchy  
**Components**: 
- RectTransform (40x40)
- CanvasGroup
- StatusEffectIcon script (với tất cả references đã được gán)

### 2. Editor Menu Commands
✅ **Đã tạo Editor Utility** với các menu sau:

## Hướng Dẫn Setup Nhanh

### Bước 1: Tạo Prefab từ GameObject có sẵn
```
1. Select "StatusEffectIcon" trong scene hierarchy
2. Drag vào Assets/Prefabs/UI/ để tạo prefab
3. Delete StatusEffectIcon khỏi scene
```

HOẶC sử dụng menu:
```
Menu: FD > Create > Status Effect Icon Prefab
→ Tự động tạo prefab tại Assets/Prefabs/UI/StatusEffectIcon.prefab
```

### Bước 2: Tạo Icon Database
```
Menu: FD > Create > Status Effect Icon Database
→ Tạo asset tại Assets/Resources/UI/StatusEffectIconDatabase.asset

Sau đó:
1. Select database asset
2. Right-click → "Add All Tags" (thêm tất cả GameplayTags)
3. Gán icon sprites cho từng tag:
   - Debuff_Slow → slow icon
   - State_Stunned → stun icon
   - State_Burning → fire icon
   - etc.
```

### Bước 3: Tạo Canvas (Optional - nếu chưa có)
```
Menu: FD > Create > Status Effect Canvas
→ Tự động tạo Canvas với StatusEffectContainer
```

### Bước 4: Add vào Characters/Enemies
Có 3 cách:

#### Cách 1: Menu (Nhanh nhất)
```
1. Select enemy/character GameObject
2. Right-click → FD > Add Status Effect Display
→ Tự động add AbilitySystemComponent + StatusEffectDisplayHandler
```

#### Cách 2: Inspector
```
1. Select character/enemy
2. Add Component → StatusEffectDisplayHandler
3. Component sẽ tự động tìm Canvas và setup
```

#### Cách 3: Code
```csharp
// Trong character script
void Awake()
{
    gameObject.AddComponent<StatusEffectDisplayHandler>();
}
```

## Cấu Hình StatusEffectDisplayManager

Sau khi setup, bạn cần gán references:

1. **Find StatusEffectContainer** trong scene (child của Canvas)
2. **Select character** có StatusEffectDisplayHandler
3. **Trong Inspector**:
   - Icon Prefab → Drag StatusEffectIcon prefab
   - Icon Container → Drag StatusEffectContainer từ Canvas
   - Icon Database → Drag StatusEffectIconDatabase asset
   - World Camera → Drag Main Camera (hoặc để null để tự động tìm)

## Test Nhanh

### Test trong Play Mode:
```csharp
// Apply một effect có tag
var slowEffect = ScriptableObject.CreateInstance<GameplayEffect>();
slowEffect.grantedTags = new[] { GameplayTag.Debuff_Slow };
slowEffect.durationType = EGameplayEffectDurationType.Duration;
slowEffect.durationMagnitude = 5f;

var asc = enemy.GetComponent<AbilitySystemComponent>();
asc.ApplyGameplayEffectToSelf(slowEffect);

// Icon sẽ tự động xuất hiện trên đầu enemy!
```

## Tùy Chỉnh

### Thay đổi vị trí icons
```csharp
[SerializeField] private Vector3 worldOffset = new Vector3(0, 3f, 0); // Cao hơn
```

### Thay đổi số lượng icons
```csharp
[SerializeField] private int maxVisibleIcons = 8; // Nhiều hơn
```

### Thay đổi khoảng cách
```csharp
[SerializeField] private float iconSpacing = 10f; // Sát nhau hơn
```

### Custom icon style
Edit StatusEffectIcon prefab:
- IconBackground: Thay đổi sprite/color
- FillImage: Thay đổi color/material
- StackText: Thay đổi font/size/color
- TimerText: Thay đổi font/size/color

## Troubleshooting

### Icons không hiện?
1. ✓ Check StatusEffectIconDatabase có icons chưa
2. ✓ Check effect có `grantedTags` chưa
3. ✓ Check StatusEffectDisplayManager có references đầy đủ
4. ✓ Check Canvas có active không

### Vị trí sai?
- Điều chỉnh `worldOffset` trong StatusEffectDisplayHandler
- Check Camera reference đúng chưa

### Performance?
- Giảm `maxVisibleIcons` (default: 6)
- Optimize icon sprites (compress, mipmap)

## Next Steps

1. **Tạo icons cho tất cả status effects**
   - Slow, Stun, Burn, Poison, Freeze, etc.
   - Gán vào StatusEffectIconDatabase

2. **Add vào tất cả enemies**
   - Select all enemy prefabs
   - Right-click → FD > Add Status Effect Display

3. **Test với các effects**
   - Apply slow, stun, burn effects
   - Verify icons hiển thị đúng
   - Verify timer đếm ngược đúng

4. **Polish**
   - Add sound effects khi apply
   - Add particle effects
   - Tối ưu performance cho mobile

---

**Files Created:**
- ✅ StatusEffectIcon.cs
- ✅ StatusEffectDisplayManager.cs  
- ✅ StatusEffectIconDatabase.cs
- ✅ StatusEffectDisplayHandler.cs
- ✅ StatusEffectSetupUtility.cs (Editor)
- ✅ StatusEffectIcon GameObject in scene

**Next: Chạy các menu commands để tạo prefab và database!**
