# GAS Modularization — Git Submodule Plan

> Mục tiêu: Tách toàn bộ hệ thống Gameplay Ability System (GAS) thành một **Git Submodule** độc lập,
> lưu tại `Assets/Packages/com.abel.gas`, kiểm thử qua Mini-game **Orb Combat** trước khi tái tích hợp vào game chính.

---

## 📐 Kiến trúc đích

```
GASFarmDefense/
└── Assets/
    ├── Packages/
    │   └── com.abel.gas/          ← Git Submodule (repo riêng)
    │       ├── Runtime/
    │       │   ├── Core/          ← ASC, AbilitySystemLogic, Data, Tags...
    │       │   ├── Abilities/     ← GameplayAbilityData, IAbilityBehaviour, Registry...
    │       │   ├── Attributes/    ← AttributeSet base, GameplayAttribute, ScalableFloat...
    │       │   ├── Effects/       ← GameplayEffect, ActiveEffect, EffectService...
    │       │   ├── Events/        ← IGASEventBus, GameplayTagChangedEvent...
    │       │   └── com.abel.gas.asmdef
    │       ├── Editor/
    │       │   └── com.abel.gas.editor.asmdef
    │       ├── Samples~/
    │       │   └── OrbCombat/     ← Mini-game test (không compile vào production)
    │       ├── package.json
    │       └── README.md
    └── _Master/
        ├── GAS/                   ← XÓA sau Phase 4 (replaced by submodule)
        └── Scripts/
            └── GAS/               ← Project-specific GAS (FDAttributeSet, AppGASInitializer...)
```

### Namespace conventions (đích):
| Lớp | Namespace cũ | Namespace mới |
|---|---|---|
| Core GAS (ASC, Logic, Data) | `GAS` | `Abel.GAS` |
| Ability interfaces & Registry | `GAS` | `Abel.GAS.Abilities` |
| Attributes base classes | `GAS` | `Abel.GAS.Attributes` |
| Effect system | `GAS` | `Abel.GAS.Effects` |
| ~~Event bus interface~~ | ~~`FD`~~ | ❌ **KHÔNG đưa vào module** — dùng C# `event` delegates |
| Debug/Logger interface | global | `Abel.GAS.Diagnostics` |
| Enums (EGameplayAttributeType) | `GAS` | `Abel.GAS` |
| **Project-specific (FD)** | `FD.Ability` | `FD.Ability` (giữ nguyên) |
| Enums game-specific (EArmorType, EDamageType) | `FD.Ability` | `FD.Ability` (giữ nguyên) |

---

## 🔍 Phân tích Coupling hiện tại (phải giải quyết)

| File trong Core GAS | Phụ thuộc FD | Hành động |
|---|---|---|
| `AbilitySystemLogic.cs` | `FD.IEventBus` | ✅ **Xóa hoàn toàn** — thay bằng C# `event` delegates trên ASC |
| `AbilitySystemComponent.cs` | `IDebugService` (global) | Thay bằng `IGASLogger` trong module |
| `GameplayEffect.cs` | `FD.Ability.DamageCalculationBase` | Move `DamageCalculationBase` vào module (là base class, không phải FD-specific) |
| `GameplayEffectModifier` | `FD.Ability.DamageCalculationBase` | Đồng bộ với dòng trên |
| `GASInitializer.cs` | `FD.Abilities.*` (hard-coded registrations) | Tách ra `AppGASInitializer.cs` ngoài module |
| `EArmorType.cs`, `EDamageType.cs` | namespace `FD.Ability` | Giữ nguyên trong project (FD-specific), KHÔNG đưa vào module |
| `FDAttributeSet.cs` | namespace `FD.Ability` | Giữ nguyên trong project |
| `FDGameplayEffectContext.cs` | extends `GameplayEffectContext` | Giữ nguyên trong project |
| `MainCharacter.cs` | `FD.Ability`, `FD.Player` | Giữ nguyên trong project, move ra khỏi GAS folder |
| `DamageTypeModifierTable.cs` | namespace `FD.Ability` | Giữ nguyên trong project |

---

## 🚀 Phase 1 — Chuẩn bị & Tạo Git Repo cho Module

**Mục tiêu**: Tạo Git repository riêng cho `com.abel.gas`, thiết lập cấu trúc folder chuẩn UPM.

### Tasks:
- [ ] **Tạo Git repo mới**: `git init` tại một folder trống, đặt tên repo `com.abel.gas`.
- [ ] **Tạo cấu trúc folder**:
  ```
  com.abel.gas/
  ├── Runtime/Core/
  ├── Runtime/Abilities/
  ├── Runtime/Attributes/
  ├── Runtime/Effects/
  ├── Runtime/Events/
  ├── Editor/
  ├── Samples~/OrbCombat/
  ├── package.json
  └── README.md
  ```
- [ ] **Tạo `package.json`** với `name: "com.abel.gas"`, `version: "1.0.0"`.
- [ ] **Tạo `.asmdef`** tại `Runtime/com.abel.gas.asmdef` (references: `VContainer`, `R3`).
- [ ] **Tạo `.asmdef`** tại `Editor/com.abel.gas.editor.asmdef`.
- [ ] **Add Submodule** vào project chính: `git submodule add <url> Assets/Packages/com.abel.gas`.
- [ ] **Tạo `.gitmodules`** entry tương ứng.

### Test Cases:
- **TC 1.1**: Folder `Assets/Packages/com.abel.gas` tồn tại và được git tracking là submodule.
- **TC 1.2**: `package.json` hợp lệ, Unity nhận diện package trong Package Manager.
- **TC 1.3**: Cả 2 `.asmdef` compile sạch (không có script nào bên trong chúng ta lỗi).

---

## 🔧 Phase 2 — Core Decoupling (Tách khỏi namespace FD)

**Mục tiêu**: Loại bỏ toàn bộ coupling giữa GAS Core và project-specific code. Sau phase này, Core GAS không còn biết đến namespace `FD`.

### Tasks:
#### 2A — Quyết định về EventBus: Dùng C# event delegates thay vì IGASEventBus

> **Lý do bỏ IGASEventBus**: Mỗi project có hệ thống event bus riêng (R3, UniRx, custom...). Nếu module
> định nghĩa `IGASEventBus`, mọi project phải implement/bridge interface này — tạo ra friction không cần thiết.
> GAS chỉ cần **notify** ra ngoài, không cần quan tâm ai lắng nghe hay lắng nghe bằng cơ chế nào.

- [ ] **Thêm C# `event` delegates vào `AbilitySystemComponent.cs`**:
  ```csharp
  // AbilitySystemComponent — GAS notifies, project subscribes however it wants
  public event Action<GameplayTag, int> OnTagChanged;      // (tag, newCount)
  public event Action<GameplayEffectAppliedEvent> OnEffectApplied;
  ```
- [ ] **Sửa `AbilitySystemLogic.cs`**: Thay 4 lệnh `_eventBus.Publish(...)` bằng callback delegate:
  ```csharp
  // Thay vì: _eventBus.Publish(new GameplayTagChangedEvent(...))
  // Dùng:    asc.OnTagChanged?.Invoke(tag, newCount);
  ```
- [ ] **Xóa `_eventBus` field** và constructor parameter `IEventBus` khỏi `AbilitySystemLogic`.
- [ ] **Xóa folder `Runtime/Events/`** (và `IGASEventBus.cs`) ra khỏi module hoàn toàn.
- [ ] **Tạo `IGASLogger.cs`** trong `Runtime/Diagnostics/`:
  ```csharp
  namespace Abel.GAS.Diagnostics {
      public interface IGASLogger {
          void Log(string message);
          void LogWarning(string message);
          void LogError(string message);
      }
  }
  ```
- [ ] **Move `DamageCalculationBase.cs`** vào `Runtime/Effects/` (đổi namespace thành `Abel.GAS.Effects`).

#### 2B — Sửa Core Files (Xóa coupling FD)
- [ ] **`AbilitySystemLogic.cs`**: Xóa `FD.IEventBus`, thay bằng gọi `asc.OnTagChanged?.Invoke(...)` và `asc.OnEffectApplied?.Invoke(...)`.
- [ ] **`AbilitySystemComponent.cs`**: Thay `IDebugService` → `IGASLogger` (inject optional). Thêm 2 public events.
- [ ] **`GameplayEffect.cs`**: Đổi type `customCalculation` từ `FD.Ability.DamageCalculationBase` → `Abel.GAS.Effects.DamageCalculationBase`.
- [ ] **Xóa `GASInitializer.cs`** khỏi Core.

#### 2C — Rename Namespace toàn bộ Core
- [ ] Thay `namespace GAS` → `namespace Abel.GAS` trong tất cả file Core.
- [ ] Thay `namespace GAS.Ability` → `namespace Abel.GAS.Abilities`.
- [ ] Cập nhật tất cả `using GAS` → `using Abel.GAS` trong Core.

### Test Cases:
- **TC 2.1**: `grep -r "using FD" Assets/Packages/com.abel.gas` → 0 results.
- **TC 2.2**: `grep -r "namespace FD" Assets/Packages/com.abel.gas` → 0 results.
- **TC 2.3**: `grep -r "IEventBus" Assets/Packages/com.abel.gas` → 0 results.
- **TC 2.4**: `com.abel.gas.asmdef` compile thành công không lỗi (trong Unity Console).

---

## 📦 Phase 3 — Migration: Copy Core Scripts vào Module

**Mục tiêu**: Di chuyển (copy + sửa namespace) từng nhóm file vào cấu trúc folder mới của module.

### Tasks:
#### 3A — Nhóm Core
- [ ] Copy → `Runtime/Core/`:
  - `AbilitySystemComponent.cs`, `AbilitySystemData.cs`, `AbilitySystemLogic.cs`
  - `AbilitySystemExtensions.cs`, `IAbilitySystemComponent.cs`
  - `IGASAvatar.cs` (TransformAvatar)
  - `GameplayTag.cs`, `GameplayTagChangedEvent.cs`

#### 3B — Nhóm Abilities
- [ ] Copy → `Runtime/Abilities/`:
  - `GameplayAbilityData.cs`, `GameplayAbilitySpec.cs`, `GameplayAbilityLogic.cs`
  - `IAbilityBehaviour.cs`, `AbilityBehaviourRegistry.cs`
  - `AbilityScalableFloat.cs` (ScalableFloat base)

#### 3C — Nhóm Attributes
- [ ] Copy → `Runtime/Attributes/`:
  - `AttributeSet.cs`, `GameplayAttribute.cs`
  - `AttributeReflectionHelper.cs`, `CsvCurveTable.cs`
  - `EGameplayAttributeType.cs` (enum base — FD có thể define thêm enum riêng)
  - `CustomAttributeSetExample.cs` (rename thành `AttributeSetTemplate.cs`)

#### 3D — Nhóm Effects
- [ ] Copy → `Runtime/Effects/`:
  - `GameplayEffect.cs`, `ActiveGameplayEffect.cs`
  - `GameplayEffectService.cs`, `GameplayEffectCalculationService.cs`
  - `GameplayEffectContext.cs`, `DamageCalculationBase.cs`

#### 3E — Diagnostics (không có Events nữa)
- [ ] Copy → `Runtime/Diagnostics/`: `IGASLogger.cs`, `GASPerformanceStats.cs`
- [ ] **Không tạo folder `Runtime/Events/`** — đã quyết định không định nghĩa EventBus trong module.

#### 3F — Xác nhận không có gì bị bỏ sót
- [ ] So sánh danh sách file cũ vs file mới đã copy.
- [ ] Xóa file cũ trong `GAS/Scripts/Base/` (giữ lại folder cũ đến Phase 5).

### Test Cases:
- **TC 3.1**: Module compile sạch với tất cả scripts mới.
- **TC 3.2**: Không có file `.cs` nào trong `Runtime/` còn `using FD`.
- **TC 3.3**: Namespace `Abel.GAS` xuất hiện đúng trong tất cả file đã copy.

---

## 🎮 Phase 4 — Mini-Game "Orb Combat" (Kiểm thử độc lập)

**Mục tiêu**: Sử dụng **chỉ duy nhất** `com.abel.gas` để xây dựng một mini-game test bed hoạt động hoàn chỉnh. Nếu Phase này thành công = Module đã thực sự độc lập.

### Thiết kế Orb Combat:
| Thành phần | Mô tả |
|---|---|
| Scene | `Samples~/OrbCombat/Scenes/OrbCombat.unity` |
| Player | Orb xanh, điều khiển bằng buttons |
| Enemy | Orb đỏ, AI tự động tấn công |
| AttributeSet | `OrbAttributeSet` (Health, Mana, Speed) |
| Abilities | Fireball, Poison, Stun, Haste |
| UI | Health bar, Mana bar, Tag list, Cooldown timer |

### Tasks:
- [ ] Tạo `OrbAttributeSet.cs` kế thừa `Abel.GAS.Attributes.AttributeSet`.
- [ ] Tạo 4 Ability Data ScriptableObjects:
  - `FireballData` (Instant, costs 20 Mana, 2s cooldown)
  - `PoisonData` (Duration 5s, DOT, stackable x3)
  - `StunData` (Duration 2s, grants tag `State.Stunned`)
  - `HasteData` (Duration 5s, +50% Speed)
- [ ] Tạo 4 Behaviour classes tương ứng.
- [ ] Tạo `OrbGameInitializer.cs` (implement `IStartable`) để đăng ký 4 ability trên vào `AbilityBehaviourRegistry`.
- [ ] Tạo `OrbDebugUI.cs` hiển thị trạng thái realtime.
- [ ] Viết `OrbCombatLifetimeScope.cs` cấu hình VContainer cho scene này.

### Test Cases:
- **TC 4.1 (Instant Damage)**: Bấm Fireball → Mana giảm 20, Enemy Health giảm đúng lượng, Cooldown 2s.
- **TC 4.2 (Duration Effect)**: Apply Poison → Health giảm mỗi giây, hết 5s effect tự xóa.
- **TC 4.3 (Stacking)**: Apply Poison 3 lần → Stack count = 3, damage cộng dồn, không tạo instance thứ 4.
- **TC 4.4 (Tag Blocking)**: Apply Stun → Player không thể cast Fireball khi bị Stun.
- **TC 4.5 (Attribute Buff)**: Apply Haste → Speed tăng đúng %, sau 5s về lại giá trị cũ.
- **TC 4.6 (Attribute Aggregation)**: Apply 2 Haste buff → Speed cộng dồn đúng.
- **TC 4.7 (Event Bus)**: Tag changed event được fire khi Add/Remove tag.

---

## 🔄 Phase 5 — Project Migration (Re-integration vào GASFarmDefense)

**Mục tiêu**: Main Project sử dụng `com.abel.gas` thay thế toàn bộ code GAS cũ trong `_Master/GAS`.

### Tasks:
#### 5A — Sửa Project-specific classes (trong `_Master/Scripts/GAS/`)
- [ ] **`FDAttributeSet.cs`**: Đổi `using GAS` → `using Abel.GAS`, kế thừa từ `Abel.GAS.Attributes.AttributeSet`.
- [ ] **`FDGameplayAbility.cs`**: Đổi namespace reference.
- [ ] **`FDGameplayEffectContext.cs`**: Kế thừa từ `Abel.GAS.Effects.GameplayEffectContext`.
- [ ] **`DamageCalculationBase.cs` (WC3)**: Kế thừa từ `Abel.GAS.Effects.DamageCalculationBase`.
- [ ] **`MainCharacter.cs`**: Move ra `_Master/Scripts/Core/Player/`, sửa references.

#### 5B — Tạo AppGASInitializer
- [ ] Tạo `AppGASInitializer.cs` trong `_Master/Scripts/GAS/` để đăng ký tất cả Ability của FD:
  ```csharp
  // Thay thế GASInitializer.cs cũ
  registry.Register<TDTowerNormalAttackData>(new TowerNormalAttackBehaviour(...));
  registry.Register<TD_InstantAoEAbilityData>(new AoEApplyEffectBehaviour(...));
  // etc.
  ```

#### 5C — Subscribe lại vào C# events của ASC (thay thế EventBus)
- [ ] **`Tower.cs`**: Thay `eventBus.Subscribe<GameplayEffectAppliedEvent>(...)` bằng subscribe trực tiếp vào ASC:
  ```csharp
  // Thay vì (cũ):
  eventBus.Subscribe<GameplayEffectAppliedEvent>(HandleEffectApplied);
  // Dùng (mới):
  _asc.OnEffectApplied += HandleEffectApplied;
  // Cleanup:
  _asc.OnEffectApplied -= HandleEffectApplied;
  ```
- [ ] **`StatusEffectVFXController.cs`**: Thay R3 subscription từ `eventBus.Receive<GameplayTagChangedEvent>()` bằng subscribe vào `asc.OnTagChanged` trực tiếp:
  ```csharp
  // Thay vì (cũ — cần IEventBus + R3 filter):
  eventBus.Receive<GameplayTagChangedEvent>()
      .Where(evt => evt.OwnerInstanceID == _targetInstanceID)
      .Subscribe(OnGameplayTagChanged);
  // Dùng (mới — direct subscribe, zero dependency):
  _trackedAsc.OnTagChanged += OnGameplayTagChanged; // (tag, newCount)
  ```
- [ ] **Xóa `FD.IEventBus` injection** khỏi constructor của `Tower.cs` và `StatusEffectVFXController.cs`.

#### 5D — Implement `IGASLogger` Bridge
- [ ] Tạo `FDLoggerBridge.cs` trong project để wrap `IDebugService` thành `Abel.GAS.Diagnostics.IGASLogger`.

#### 5E — Cleanup
- [ ] Xóa toàn bộ folder `_Master/GAS/Scripts/Base/`.
- [ ] Giữ lại `_Master/GAS/_Demo/` để tham khảo.
- [ ] Chạy full build & test.

### Test Cases:
- **TC 5.1**: Project compile thành công, 0 error.
- **TC 5.2**: Chạy Map `TranHuongDao` — Tower bắn và apply hiệu ứng bình thường.
- **TC 5.3**: Hiệu ứng Slow (debuff speed) trên Enemy vẫn hoạt động.
- **TC 5.4**: Hệ thống Cooldown Tower vẫn đúng.
- **TC 5.5**: Editor Debug View vẫn hiển thị ASC stats.

---

## 🗓️ Timeline (Dự kiến)

| Phase | Mô tả | Thời gian |
|---|---|---|
| Phase 1 | Git Setup + Folder Structure | 0.5 ngày |
| Phase 2 | Core Decoupling (Xóa coupling FD) | 1 ngày |
| Phase 3 | Migration Scripts vào Module | 1 ngày |
| Phase 4 | Orb Combat Mini-game | 1.5 ngày |
| Phase 5 | Re-integration vào GASFarmDefense | 1 ngày |
| **Tổng** | | **~5 ngày** |

---

## ⚠️ Rủi ro & Biện pháp

| Rủi ro | Mức độ | Biện pháp |
|---|---|---|
| ScriptableObject mất GUID khi move file | Cao | Dùng `git mv` thay vì copy để giữ GUID. Nếu không được, script nạp lại GUID. |
| VContainer injection bị lỗi sau khi đổi namespace | Trung bình | Test từng service một trong Orb Combat trước khi migrate. |
| `EGameplayAttributeType` bị duplicate (Core vs FD) | Trung bình | Core giữ enum base, FD project có thể extend bằng string key. |
| Assembly Reference vòng lặp | Thấp | Đảm bảo chỉ có 1 chiều: FD project → `com.abel.gas`, không có chiều ngược. |
