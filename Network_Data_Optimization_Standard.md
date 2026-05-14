# Tiêu chuẩn Tối ưu hóa Dữ liệu Mạng & Bộ nhớ (Network Data Optimization Standard)

Tài liệu này quy định các tiêu chuẩn bắt buộc khi thiết kế các cấu trúc dữ liệu (Structs/Payloads) truyền qua mạng hoặc lưu trữ trong bộ nhớ hiệu suất cao. **AI và Lập trình viên phải tuân thủ nghiêm ngặt các quy tắc này.**

---

## 1. Quy tắc Sắp xếp (Field Ordering)
**Quy tắc**: Sắp xếp các biến theo kích thước giảm dần (Largest to Smallest).
**Lý do**: Tránh việc CPU tự động chèn Padding (khoảng trống vô ích) để căn chỉnh bộ nhớ.

- **Thứ tự ưu tiên**: `double/long (8b)` > `float/int/uint (4b)` > `short/ushort (2b)` > `byte/bool (1b)`.

| Cấu trúc Tồi (24 bytes) | Cấu trúc Tối ưu (16 bytes) |
| :--- | :--- |
| `bool b1;` (1b + 3b padding) | `int i1;` (4b) |
| `int i1;` (4b) | `short s1;` (2b) |
| `bool b2;` (1b + 1b padding) | `bool b1;` (1b) |
| `short s1;` (2b) | `bool b2;` (1b) |

---

## 2. Lượng tử hóa Dữ liệu (Data Quantization)
Hãy chọn kiểu dữ liệu nhỏ nhất có thể mà vẫn đảm bảo độ chính xác cần thiết.

### A. Màu sắc (Colors)
- **KHÔNG** dùng `UnityEngine.Color` (16 bytes - 4 floats).
- **LUÔN** dùng `UnityEngine.Color32` (4 bytes - 4 bytes).
- *Tiết kiệm*: **75%**.

### B. Số thực (Floating Points)
- Nếu giá trị nằm trong khoảng `0.0` đến `1.0` (như độ trong suốt, cường độ shake):
    - Chuyển sang `byte` (0-255). Công thức: `byteValue = (byte)(floatValue * 255)`.
    - *Tiết kiệm*: **75%** (từ 4 bytes xuống 1 byte).
- Nếu cần độ chính xác vừa phải: Dùng `Half` (2 bytes) thay cho `Float` (4 bytes).

### C. Enums
- Luôn chỉ định kiểu cơ sở là `byte` cho các Enum có ít hơn 256 phần tử.
```csharp
public enum ECharacterState : byte { Idle, Move, Attack }
```

---

## 3. Đóng gói Bit (Bit Packing)
Dùng cho các biến Boolean hoặc các số nguyên nhỏ.

- Thay vì dùng 8 biến `bool` (8 bytes), hãy dùng một biến `byte` kết hợp với **Bitmask** (1 byte).
- *Tiết kiệm*: **87.5%**.

---

## 4. Kiểm soát Bố cục (Struct Layout)
Sử dụng `System.Runtime.InteropServices` để ép buộc cấu trúc bộ nhớ.

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OptimizedPayload { ... }
```

- `Pack = 1`: Triệt tiêu hoàn toàn padding giữa các biến.
- `LayoutKind.Explicit`: Dùng khi muốn các biến "chồng" lên nhau (Union) hoặc quy định chính xác địa chỉ byte.

---

## 5. Tránh các kiểu dữ liệu Quản lý (Managed Types)
Trong Payload truyền mạng, **CẤM** sử dụng các kiểu dữ liệu sau:
- `string`: Thay bằng `FixedString32Bytes` (nếu dùng Unity Collections) hoặc mảng byte cố định.
- `class`: Chỉ sử dụng `struct`.
- `array[]`: Thay bằng `fixed byte[Size]` hoặc các cấu trúc mảng cố định.

---

## 6. Checklist khi thiết kế một Payload mới
1. [ ] Các biến đã sắp xếp từ lớn đến nhỏ chưa?
2. [ ] Có biến `float` nào có thể chuyển sang `byte` hoặc `short` không?
3. [ ] Có biến `int` nào có thể chuyển sang `byte` không?
4. [ ] Các biến `bool` có nên đóng gói thành một `byte` bitmask không?
5. [ ] Đã sử dụng `Color32` thay cho `Color` chưa?
6. [ ] Enum đã được định nghĩa là `: byte` chưa?

---
**Ghi chú**: Tối ưu hóa dữ liệu không chỉ là tiết kiệm băng thông, mà còn giúp giảm tải CPU khi xử lý các gói tin (Serialization/Deserialization) và cải thiện Cache Locality.
