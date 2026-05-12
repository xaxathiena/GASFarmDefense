# Master Plan: GAS Completion & Full Verification [PHASE 1-4 DONE]

> **Quy tắc**: Mỗi Phase phải pass 100% Test Cases trước khi chuyển sang Phase tiếp theo. 
> Toàn bộ quá trình thực hiện phải được ghi log kết quả vào file `verification_report.md`.

---

## 🚀 LỘ TRÌNH CHI TIẾT & TEST CASES

### PHASE 1: Attribute & Modifier Math [DONE]
*Mục tiêu: Xác minh tính chính xác của Aggregation Pipeline.*

- [x] **Task 1.1**: UI hiển thị Real-time Modifiers list.
- [x] **Task 1.2**: Implement Custom Clamping (e.g. Health không vượt MaxHealth).

| ID | Test Case | Input | Expected Output | Status |
|---|---|---|---|---|
| TC1.1 | Aggregation Order | Speed=5, Add(+2), Multiply(2.0) | Speed = 14.0 | ✅ |
| TC1.2 | Override Priority | Apply Override(10) | Speed = 10 | ✅ |
| TC1.3 | Max Clamping | HP=90, Heal(+50) | HP = 100 | ✅ |
| TC1.4 | Min Clamping | HP=10, Damage(-50) | HP = 0 | ✅ |

---

### PHASE 2: Gameplay Effect Lifecycle [DONE]
*Mục tiêu: Xác minh vòng đời Effect và Periodic execution.*

- [x] **Task 2.1**: Implement Timer UI cho active effects.
- [x] **Task 2.2**: Implement Dispel logic (Remove by Tag).

| ID | Test Case | Input | Expected Output | Status |
|---|---|---|---|---|
| TC2.1 | Duration Expiry | Apply Slow 3s | Speed hồi phục sau 3s | ✅ |
| TC2.2 | Periodic DoT | Poison: 10 dmg / 1s (3s) | Mất 30 HP sau 3 nhịp | ✅ |
| TC2.3 | Tag Removal | Remove Tag `Debuff` | Effects bị xóa theo Tag | ✅ |
| TC2.4 | Infinite Effect | Apply Buff Infinite | Buff tồn tại vĩnh viễn | ✅ |

---

### PHASE 3: Stacking Logic [DONE]
*Mục tiêu: Xác minh cơ chế cộng dồn (Max Stacks, Scaling).*

- [x] **Task 3.1**: Verify Max Stacks clamping.
- [x] **Task 3.2**: Verify Individual Stack Duration policy.
- [x] **Task 3.3**: Verify Refresh Duration policy.

| ID | Test Case | Input | Expected Output | Status |
|---|---|---|---|---|
| TC3.1 | Max Stacks | Add 10 stacks (Max 3) | Chỉ nhận tối đa 3 stacks | ✅ |
| TC3.2 | Individual Expiry | 3 stacks (mỗi cái 3s) | Rụng dần từng stack mỗi 1s | ✅ |
| TC3.3 | Refresh Duration | Thêm stack mới ở giây thứ 4 | Timer của cả stack quay về 5s | ✅ |

---

### PHASE 4: Gameplay Events & Tags [DONE]
*Mục tiêu: Xác minh cơ chế Event-driven và Tag blocking.*

- [x] **Task 4.1**: Test Trigger Ability by Tag (OwnedTagAdded).
- [x] **Task 4.2**: Test Ability Blocking (Ability bị chặn bởi Tag Stun).
- [x] **Task 4.3**: Gameplay Event Bus (Send Event -> Trigger Ability).

| ID | Test Case | Input | Expected Output | Status |
|---|---|---|---|---|
| TC4.1 | Auto-Trigger | Apply Tag `State.OnFire` | Ability "Self-Extinguish" tự kích hoạt | ✅ |
| TC4.2 | Tag Blocking | Apply Tag `State.Stunned` | Bấm "Cast Fireball" báo success? False | ✅ |
| TC4.3 | Event Payload | Send Event `Event.Explosion` | Ability "ExplosionReact" chạy | ✅ |

---

### PHASE 5: Gameplay Cues (VFX/SFX)
*Mục tiêu: Xác minh khả năng kích hoạt hiệu ứng hình ảnh/âm thanh qua Tag.*

- [ ] **Task 5.1**: Implement `GameplayCueManager`.
- [ ] **Task 5.2**: Implement `IGameplayCueNotify` (Static & Actor-based).
- [ ] **Task 5.3**: Test Cue Lifecycle (OnActive, WhileActive, OnRemove).

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC5.1 | Instant Cue | Gửi Tag `Cue.Fireball.Impact` | VFX nổ xuất hiện tại chỗ |
| TC5.2 | Duration Cue | Apply Buff có Tag `Cue.Shield.Loop` | VFX Khiên duy trì cùng thời gian Buff |
| TC5.3 | Sound Cue | Kích hoạt Ability có Tag `Cue.Magic.Cast` | SFX âm thanh cast phép vang lên |

---

### PHASE 6: Advanced Scenarios & Calculations
*Mục tiêu: Xác minh các cơ chế tính toán nâng cao (ScalableFloat, SetByCaller).*

- [ ] **Task 6.1**: Implement ScalableFloat verification.
- [ ] **Task 6.2**: Test Gameplay Event Payload (Magnitude passing).
- [ ] **Task 6.3**: Implement `SetByCaller` logic.

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC6.1 | Event Magnitude | Gửi Event `Heal` với Magnitude=50 | Ability nhận đúng 50 để hồi máu |
| TC6.2 | SetByCaller | Set magnitude qua tag `Data.Dmg` | Damage tính theo giá trị truyền vào |
| TC6.3 | Level Scaling | Skill Level 1 vs Level 10 | Giá trị Effect tăng theo Curve |

---

### PHASE 7: GUI All-in-One Suite
*Mục tiêu: Đóng gói thành công cụ test hoàn chỉnh.*

- [ ] Sandbox mode: Tự do chỉnh sửa Attributes/Tags/Effects của Player và Enemy.
- [ ] Visualizer: Vẽ biểu đồ thay đổi Attribute theo thời gian.
- [ ] Export Report: Xuất kết quả test ra file Markdown.
