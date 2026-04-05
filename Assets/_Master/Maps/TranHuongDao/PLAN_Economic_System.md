# Economic System — Random Farm TD
## Implementation Plan

> **Author**: Senior Game Developer Review  
> **Date**: 2026-04-04  
> **Scope**: Seed → Plant → Grow (wave-based) → Harvest → Sell → Gold

---

## 1. Tổng quan luồng

```
Wave N bắt đầu
    └─► WaveManager.OnWaveStarted(N)
            └─► TDEconomyService.HandleWaveStarted()
                    ├─► Thu hoạch cây chín (PlantedOnWave + 2 <= N)
                    └─► Roll giá thị trường mới (random trong [MarketMin, MarketMax])

Wave N kết thúc (tất cả enemy đã bị diệt)
    └─► WaveManager.OnWaveCompleted(N)
            └─► TDEconomyService.HandleWaveCompleted()
                    └─► Cấp 10 seeds cho người chơi

Prep time (giữa wave — người chơi hành động)
    └─► Mở FarmPopup → trồng seeds vào loại cây mong muốn
    └─► Mở MarketPopup → bán quả thu hoạch → nhận Gold
```

> **Lý do cấp seeds sau khi wave completed**: Người chơi có toàn bộ prep time để suy nghĩ và trồng, chứ không phải vừa chiến đấu vừa farm.

---

## 2. Data Model

### 2.1 — `CropDefinition` + `CropConfigSO` *(ScriptableObject — file mới)*

**File**: `Core/Config/CropConfig.cs`

```csharp
[Serializable]
public class CropDefinition
{
    public string CropID;        // "Carrot", "Pumpkin", "Grape"
    public string DisplayName;
    public Sprite Icon;
    public int WavesToHarvest;   // số wave phải chờ (mặc định: 2)
    public int YieldMin;         // quả tối thiểu / hạt (mặc định: 5)
    public int YieldMax;         // quả tối đa / hạt (mặc định: 10)
}

[CreateAssetMenu(menuName = "Map/RandomFarmTD/Crop Config")]
public class CropConfigSO : ScriptableObject
{
    public List<CropDefinition> crops;
    public int SeedsPerWave    = 10;    // seeds phát mỗi wave
    public float MarketPriceMin = 50f;  // giá tối thiểu toàn thị trường
    public float MarketPriceMax = 200f; // giá tối đa toàn thị trường
}
```

> MarketPriceMin/Max là **global** — áp dụng cho tất cả các loại quả như thiết kế đã định.

---

### 2.2 — `PlantedCrop` *(runtime struct — pure C#)*

```csharp
public struct PlantedCrop
{
    public string CropID;
    public int PlantedOnWave;   // wave lúc trồng
    public int HarvestOnWave;   // PlantedOnWave + WavesToHarvest
    public int SeedCount;       // số hạt trồng trong lần này
    // Yield sẽ tính khi harvest — không pre-compute để giữ suspense
}
```

Nếu người chơi trồng 3 hạt Carrot trong 1 lần → 1 entry duy nhất với `SeedCount = 3` — gọn hơn, dễ quản lý.

---

### 2.3 — `PriceHistory` *(runtime — dùng cho chart UI)*

```csharp
// Lưu lịch sử giá của N wave gần nhất để vẽ chart
private const int MaxPriceHistoryWaves = 8; // 8 wave gần nhất

// Key: CropID, Value: queue giá theo thứ tự wave (oldest → newest)
private readonly Dictionary<string, Queue<float>> _priceHistory = new();

// Khi RollMarketPrices() chạy:
// 1. Enqueue giá mới vào queue của từng crop
// 2. Nếu Count > MaxPriceHistoryWaves → Dequeue (bỏ wave cũ nhất)
```

UI chart sẽ đọc `GetPriceHistory(cropID)` → trả về `IReadOnlyList<float>` theo thứ tự thời gian.

---

## 3. Refactor `TDEconomyService`

### ❌ Phải bỏ

- `UniTask WaitAndHarvest(string)` — harvest theo timer realtime → **không đúng thiết kế**
- `GrowingCrops` dictionary đơn giản → không lưu được wave planted

### ✅ State mới

```csharp
private readonly List<PlantedCrop> _plantedCrops = new();
private readonly Dictionary<string, float> _currentPrices = new();
// Inventory giữ nguyên: Dictionary<string, int>
// Gold giữ nguyên
// Seeds: không còn hardcode 50 → được cấp qua GrantSeeds()
```

### ✅ API mới

```csharp
// Gọi từ FarmPopup
public bool TryPlantSeeds(string cropID, int seedCount, int currentWave);

// Gọi từ MarketPopup
public bool TrySellCrop(string cropID, int quantity, out int goldEarned);

// UI queries
public float GetCurrentPrice(string cropID);
public IReadOnlyList<PlantedCrop> GetPlantedCrops();
```

### ✅ Tích hợp WaveManager

```csharp
public TDEconomyService(CropConfigSO cropConfig, IWaveManager waveManager, FD.IEventBus eventBus)
{
    waveManager.OnWaveStarted   += HandleWaveStarted;
    waveManager.OnWaveCompleted += HandleWaveCompleted;
}

private void HandleWaveStarted(int waveIndex)
{
    HarvestReadyCrops(waveIndex);  // cây chín → vào Inventory
    RollMarketPrices();            // giá mới cho wave này
}

private void HandleWaveCompleted(int waveIndex)
{
    GrantSeeds();                  // +10 seeds
}
```

---

## 4. Market Price System

```csharp
private void RollMarketPrices()
{
    foreach (var crop in _cropConfig.crops)
    {
        float price = Random.Range(_cropConfig.MarketPriceMin, _cropConfig.MarketPriceMax);
        _currentPrices[crop.CropID] = Mathf.Round(price);
    }
    _eventBus.Publish(new MarketPricesUpdatedEvent(_currentPrices));
    OnDataChanged?.Invoke();
}
```

Giá **giữ nguyên trong suốt 1 wave** — chỉ roll lại khi wave mới bắt đầu.

---

## 5. Harvest Logic

```csharp
private void HarvestReadyCrops(int currentWave)
{
    for (int i = _plantedCrops.Count - 1; i >= 0; i--)
    {
        var crop = _plantedCrops[i];
        if (crop.HarvestOnWave > currentWave) continue;

        var def = GetDefinition(crop.CropID);
        int totalYield = 0;
        for (int s = 0; s < crop.SeedCount; s++)
            totalYield += Random.Range(def.YieldMin, def.YieldMax + 1); // inclusive max

        Inventory[crop.CropID] = Inventory.GetValueOrDefault(crop.CropID) + totalYield;
        _plantedCrops.RemoveAt(i);

        _eventBus.Publish(new CropHarvestedEvent(crop.CropID, totalYield));
    }
    OnDataChanged?.Invoke();
}
```

---

## 6. Events (qua FD.IEventBus)

| Event struct | Publish khi | Payload |
|---|---|---|
| `SeedsGrantedEvent` | Sau wave cleared | `int Count` |
| `CropHarvestedEvent` | Đầu wave mới, cây chín | `string CropID, int Yield` |
| `MarketPricesUpdatedEvent` | Đầu wave mới | `IReadOnlyDictionary<string,float>` |
| `CropSoldEvent` | Người chơi bán | `string CropID, int Qty, int Gold` |
| `CropPlantedEvent` | Người chơi trồng | `string CropID, int SeedCount` |

Tất cả đi qua `FD.IEventBus` — không cần thêm event system mới.

---

## 7. UI

### FarmPopup *(sửa file có sẵn)*

Hiển thị **preview yield của wave tiếp theo**: với mỗi batch `PlantedCrop` có `HarvestOnWave == currentWave + 1`, UI hiển thị range dự kiến:

```
[ Seeds còn lại: 7 ]   (tích lũy, không bao giờ mất)

[ 🥕 Carrot  ] [ Trồng: [__3__] hạt ] [ Chờ 2 wave ] [BTN Trồng]
[ 🎃 Pumpkin ] [ Trồng: [__2__] hạt ] [ Chờ 2 wave ] [BTN Trồng]
[ 🍇 Grape   ] [ Trồng: [__2__] hạt ] [ Chờ 2 wave ] [BTN Trồng]

── Đang trồng ──────────────────────────────────────────
  🥕 Carrot  x3  │ Wave tiếp theo ★  │ Dự kiến: 15–30 quả
  🎃 Pumpkin x2  │ Còn 2 wave        │ Dự kiến: 10–20 quả
```

**Preview logic** (hiển thị bên cạnh mỗi batch):
- `Wave tiếp theo ★` → `HarvestOnWave == currentWave + 1`
- `Dự kiến: X–Y quả` → `SeedCount * YieldMin` đến `SeedCount * YieldMax`
- Màu xanh / highlight nếu sắp chín wave tới
- Trồng được bất cứ lúc nào (kể cả trong battle)

### MarketPopup *(sửa file có sẵn)*

```
── Giá hôm nay (Wave 4) ─────────────────────────────
  🥕 Carrot   :  143g / quả  ▲ +23
  🎃 Pumpkin  :   87g / quả  ▼ -12
  🍇 Grape    :  178g / quả  ▲ +55

  [📈 Xem chart lịch sử giá]  ← mở chart panel

── Kho của bạn ──────────────────────────────────────
  Carrot x15   [Bán tất cả → +2145g] [Bán N...]
  Pumpkin x8   [Bán tất cả →  +696g] [Bán N...]
```

**Market chart** (panel mở rộng trong MarketPopup):
- Trục X: 8 wave gần nhất
- Trục Y: `[MarketPriceMin, MarketPriceMax]`
- Mỗi loại quả 1 đường polyline khác màu
- Vẽ bằng `LineRenderer` trong UI Toolkit (dùng VisualElement + custom paint, hoặc LineRenderer trong world-space overlay)
- Data source: `TDEconomyService.GetPriceHistory(cropID)`

---

## 8. Các file cần tạo / sửa

| Action | File | Ghi chú |
|---|---|---|
| **NEW** | `Core/Config/CropConfig.cs` | CropDefinition + CropConfigSO |
| **NEW** | `Core/Events/FarmEvents.cs` | 5 event structs |
| **NEW** | `SO/Resources/Configs/CropConfig.asset` | Config asset (tạo trong Unity) |
| **MODIFY** | `Core/TDEconomyService.cs` | Refactor toàn bộ |
| **MODIFY** | `Core/FarmRandomTDLifetimeScope.cs` | Đăng ký CropConfigSO |
| **MODIFY** | `UI/RandomFarmTD/FarmPopup.cs` | UI: seed input, growing list, wave-ahead preview |
| **MODIFY** | `UI/RandomFarmTD/MarketPopup.cs` | UI: price table, history chart, sell controls |
| **NEW** | `UI/RandomFarmTD/PriceChartElement.cs` | Custom VisualElement vẽ polyline chart |

---

## 9. Thứ tự thực hiện

| Phase | Nội dung | Verify |
|---|---|---|
| **1 — Backend** | CropConfig + refactor TDEconomyService + DI + price history | Log: seeds granted, harvest triggered, prices rolled, history queue growing |
| **2 — Market & Sell** | TrySellCrop + MarketPopup price table + sell controls | Bán quả → gold tăng, inventory giảm |
| **3 — Farm UI** | FarmPopup: seed input + growing list + **wave-ahead yield preview** | Trồng → hiển thị dự kiến quả → chín đúng wave |
| **4 — Chart & Polish** | PriceChartElement (polyline N wave) + price trend arrows + harvest notification | Chart hiển thị đúng, UX hoàn chỉnh |

---

## 10. Answers — Đã xác nhận

> [!NOTE]
> **Q1 — Seed carryover**: ✅ Seeds **tích lũy qua các wave**, không bao giờ mất nếu không trồng. Không có max cap.

> [!NOTE]
> **Q2 — Trồng bất kỳ lúc nào**: ✅ FarmPopup có thể mở và trồng kể cả trong lúc wave đang chạy.

> [!IMPORTANT]
> **Q3 — Price history cho chart**: Lưu **8 wave gần nhất** cho mỗi loại quả bằng `Dictionary<string, Queue<float>>`. Chart UI vẽ polyline từ data này. Cần thêm `PriceChartElement.cs` (custom VisualElement dùng `MeshGenerationContext` của UI Toolkit để vẽ line).

> [!IMPORTANT]
> **Q4 — Wave-ahead preview**: FarmPopup hiển thị **dự kiến yield wave tiếp theo** cho các cây đang trồng sắp chín (`HarvestOnWave == currentWave + 1`). Format: `"Dự kiến: 15–30 quả"` (= `SeedCount * YieldMin` đến `SeedCount * YieldMax`). Highlight màu xanh để player dễ nhận biết.
