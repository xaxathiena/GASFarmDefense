# Phân tích & Đề xuất Nâng cấp Hệ thống Gameplay Cue (GAS)

Tài liệu này phân tích hai vấn đề cốt lõi: Kiến trúc quản lý Asset cho Gameplay Cue và Cơ chế truyền dữ liệu (Parameters) trong môi trường mạng.

---

## 1. Hệ thống Gameplay Cue: Từ Cứng (Hard-coded) sang Dựa trên Asset (Asset-Driven)

### Phân tích ý tưởng của Anh:
Anh muốn kế thừa từ một class cụ thể, tạo Prefab, và đăng ký tập trung trong một "Asset Resource" định danh bằng Gameplay Tag.

**Ưu điểm:**
- **Designer-Friendly**: Designer có thể thay đổi VFX/SFX cho một kỹ năng chỉ bằng cách kéo thả Prefab vào Library mà không cần chạm vào code.
- **Tập trung (Centralized)**: Dễ dàng kiểm soát toàn bộ hiệu ứng trong game tại một chỗ duy nhất.
- **Định danh bằng Tag**: Phù hợp hoàn toàn với triết lý của GAS.

**Nhược điểm:**
- Nếu không quản lý tốt, việc load toàn bộ Library có thể gây tốn RAM.
- Cần cơ chế Pooling để tránh giật lag (GC) khi sinh/hủy VFX liên tục (ví dụ: mưa tên).

### Đề xuất Kiến trúc AAA (Professional Grade):

#### A. Gameplay Cue Library (ScriptableObject)
Thay vì đăng ký thủ công, chúng ta dùng một `GameplayCueSet` (ScriptableObject) chứa danh sách các mapping:
`GameplayTag` -> `GameplayCueNotifyObject`.

#### B. Phân loại Cue Notify (Inheritance Hierarchy)
Chúng ta nên chia làm 2 loại chính:
1.  **GameplayCueNotify_Static**: Dùng cho các hiệu ứng tức thời (Execute) như nổ, va chạm. Thường là các Object được spawn ra rồi tự biến mất.
2.  **GameplayCueNotify_Actor**: Dùng cho các hiệu ứng duy trì (OnActive/WhileActive/OnRemove) như Buff giáp, Burn. Hiệu ứng này sẽ được "attach" vào nhân vật.

#### C. Hệ thống Pooling & Asset Management
- Sử dụng **Unity Addressables** để load Cue theo yêu cầu (Async).
- Tích hợp **Object Pooling**: Khi một kỹ năng kết thúc, VFX không bị Destroy mà được đưa về Pool để tái sử dụng.

---

## 2. Gameplay Cue Parameters: Tối ưu hóa Dữ liệu & Networking

### Vấn đề của `object OptionalObject`:
Trong môi trường mạng (Multiplayer), `object` là một tham chiếu bộ nhớ cục bộ. Máy A không thể gửi "địa chỉ bộ nhớ" sang máy B được.

### Giải pháp chuyên nghiệp:

#### A. Sử dụng "Data-only Payload" (Struct-based)
Thay vì truyền `object`, chúng ta định nghĩa các trường dữ liệu nguyên thủy (primitives) có thể tuần tự hóa (Serializable):
- **InstigatorID / TargetID**: Dùng `int` (InstanceID) hoặc `NetworkID` thay vì tham chiếu trực tiếp class.
- **HitResult**: Một struct chứa tọa độ, pháp tuyến, Bone name (nơi bị trúng đạn).

#### B. Cơ chế "Byte Stream Payload" (Advanced)
Đối với các dữ liệu đặc thù (ví dụ: Màu sắc của kỹ năng, hình dạng tia sét), chúng ta dùng một mảng byte hoặc một struct Union:
```csharp
public struct GameplayCueParameters {
    public int InstigatorID;
    public Vector3 Location;
    public float Magnitude;
    public GameplayTag CueTag;
    // Thay vì object, ta dùng bitmask hoặc dữ liệu thô
    public CustomCueData CustomData; 
}
```

#### C. Quy trình đồng bộ mạng tối ưu:
1.  **Server**: Kiểm tra logic kỹ năng, quyết định kích hoạt Cue.
2.  **Broadcasting**: Server gửi một "NetMulticast" (trong FishNet hoặc Photon) chỉ chứa `CueTag` và `Parameters` (dạng nén).
3.  **Clients**: Khi nhận được Tag, Client tự tra cứu trong `CueLibrary` cục bộ để tìm Prefab tương ứng và thực hiện hiệu ứng.
    *   *Lợi ích*: Tiết kiệm băng thông cực lớn vì không phải gửi cả cái Prefab qua mạng, chỉ gửi cái "Tên" (Tag).

---

## 3. Cơ chế Generic Binary Payload (The Developer-Friendly Approach)

Để đạt được mục tiêu "Người lập trình không cần biết về cơ chế nhị phân", chúng ta sử dụng kiến trúc sau:

### A. Triết lý thiết kế
Thay vì bắt người dùng phải "Write/Read" từng field một cách thủ công, hệ thống sẽ sử dụng **Generics** và **Memory Mapping** để biến bất kỳ `struct` nào thành dữ liệu thô có thể gửi qua mạng.

### B. Cách thức triển khai kỹ thuật (AAA Implementation)

1.  **Fixed-size Buffer**: Trong `GameplayCueParameters`, thay vì `object`, ta dùng một `byte[]` hoặc `NativeArray` có kích thước cố định (ví dụ 128 bytes).
2.  **Zero-Allocation Mapping**: Sử dụng `System.Runtime.InteropServices.MemoryMarshal` để copy toàn bộ nội dung của struct vào buffer. Điều này giúp loại bỏ hoàn toàn Boxing/Unboxing.

```csharp
public struct GameplayCueParameters {
    public byte[] RawPayload; // Dữ liệu thô để gửi qua mạng

    public void SetData<T>(T data) where T : struct {
        // Biến bất kỳ struct nào thành bytes cực nhanh
        RawPayload = RawDataConverter.ToBytes(data);
    }

    public T GetData<T>() where T : struct {
        // Ép ngược từ bytes về struct T
        return RawDataConverter.FromBytes<T>(RawPayload);
    }
}
```

### C. Ví dụ thực tế: Designer & Programmer Experience

**Bước 1: Programmer định nghĩa dữ liệu đặc thù cho kỹ năng:**
```csharp
public struct ExplosionPayload {
    public float Radius;
    public float CameraShakeIntensity;
    public Color FlashColor;
}
```

**Bước 2: Gửi dữ liệu (Server-side Ability):**
```csharp
var payload = new ExplosionPayload { Radius = 10f, CameraShakeIntensity = 0.5f, FlashColor = Color.red };
parameters.SetData(payload); // Hệ thống tự động đóng gói
asc.ExecuteGameplayCue(Tag_Cue_Explosion, parameters);
```

**Bước 3: Nhận và sử dụng dữ liệu (Client-side VFX):**
```csharp
public void HandleCue(GameplayCueParameters parameters) {
    var data = parameters.GetData<ExplosionPayload>(); 
    vfx.SetScale(data.Radius);
    camera.Shake(data.CameraShakeIntensity);
}
```

---

## Gợi ý Hệ thống Tổng thể (The Vision)

1.  **Thiết kế**: Designer tạo một Prefab, gắn script `GameplayCueNotify_Burst`. Script này có các field: *VFX Particle, SFX Clip, Camera Shake*.
2.  **Đăng ký**: Designer kéo Prefab này vào `GAS_CueConfig` (ScriptableObject) và gán Tag `Cue.Skill.Fireball.Explosion`.
3.  **Kích hoạt**: Trong Code Ability, anh chỉ cần gọi:
    `ASC.ExecuteGameplayCue(GameplayTag.Cue_Fireball_Explosion, myParams);`
4.  **Vận hành**: `CueManager` nhận lệnh -> Tra cứu Config -> Lấy Prefab từ Pool -> Khởi tạo tại `myParams.Location` -> Play VFX/SFX.

Hệ thống này cực kỳ dễ bảo trì vì Code không quan tâm VFX trông như thế nào, và Designer không quan tâm Code kích hoạt khi nào.
