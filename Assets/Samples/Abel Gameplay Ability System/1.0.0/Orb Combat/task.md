# Master Plan: GAS Completion & Full Verification

> **Quy tắc**: Mỗi Phase phải pass 100% Test Cases trước khi chuyển sang Phase tiếp theo. 
> Toàn bộ quá trình thực hiện phải được ghi log kết quả vào file `verification_report.md`.

---

## 📋 PHẦN 1: KẾT QUẢ RÀ SOÁT (SYSTEM AUDIT)

| Tính năng | Trạng thái | Chi tiết kỹ thuật | Gaps cần bổ sung |
| :--- | :--- | :--- | :--- |
| **Core ASC** | ✅ Hoàn thành | `AbilitySystemComponent.cs` ổn định. | Thêm `HandleGameplayEvent`. |
| **Attributes** | ✅ Hoàn thành | Base/Current, Aggregator. | Thêm custom Clamping logic. |
| **Effects** | ✅ Hoàn thành | Instant, Duration, Infinite. | Thêm Periodic & Stacking. |
| **Gameplay Cues** | ❌ **Thiếu** | VFX/SFX trigger. | Cần `CueManager`. |
| **Gameplay Events** | ❌ **Thiếu** | Messaging system. | Cần `GameplayEventData`. |
| **Ability Tasks** | ❌ **Thiếu** | Async/Latent actions. | Cần `WaitDelay`, `WaitEvent`. |

---

## 🚀 PHẦN 2: LỘ TRÌNH CHI TIẾT & TEST CASES

### PHASE 0: Hạ tầng (Infrastructure)
*Mục tiêu: Hoàn thiện các class nền tảng để hỗ trợ các cơ chế nâng cao.*

- **Task 0.1**: Implement `GameplayEventData` & `HandleGameplayEvent`.
- **Task 0.2**: Implement `GameplayCueManager` & `IGameplayCueNotify`.
- **Task 0.3**: Implement Ability Task Runner & `WaitDelay`.
- **Task 0.4**: Implement Stacking Duration Policies.

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC0.1 | Event Trigger | Gửi Event `Combat.Hit` tới Enemy | Ability "CounterAttack" tự động kích hoạt |
| TC0.2 | Cue Activation | Thêm Tag `Cue.Fireball.Impact` | VFX Impact xuất hiện tại tọa độ xác định |
| TC0.3 | Latent Task | Ability gọi `WaitDelay(2.0s)` | Log "Effect Applied" hiện ra sau đúng 2.0s |
| TC0.4 | Stack Policy | Apply 2 stacks (Individual Duration) | Mỗi stack tự biến mất sau 5s riêng biệt |

---

### PHASE 1: Attribute & Modifier Math
*Mục tiêu: Xác minh tính chính xác của Aggregation Pipeline.*

- **Task 1.1**: UI hiển thị Real-time Modifiers list.
- **Task 1.2**: Implement Custom Clamping (e.g. Health không vượt MaxHealth).

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC1.1 | Aggregation Order | Speed=5, Add(+2), Multiply(2.0) | Speed = (5+2) * 2.0 = 14.0 |
| TC1.2 | Override Priority | Apply Override(10) trong khi có Add(+100) | Speed = 10 (Override thắng mọi Modifiers khác) |
| TC1.3 | Max Clamping | HP=90, Heal(+50), MaxHP=100 | HP = 100 (Không vượt quá MaxHP) |
| TC1.4 | Min Clamping | HP=10, Damage(-50) | HP = 0 (Không xuống số âm) |

---

### PHASE 2: Gameplay Effect Lifecycle
*Mục tiêu: Xác minh vòng đời Effect và Periodic execution.*

- **Task 2.1**: Implement Timer UI cho active effects.
- **Task 2.2**: Implement Dispel logic (Remove by Tag).

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC2.1 | Duration Expiry | Apply Slow 3s | Speed giảm 3s, sau đó tự hồi phục hoàn toàn |
| TC2.2 | Periodic DoT | Poison: 10 dmg mỗi 1s, duration 3s | Mất 10 HP tại giây 1, 2, 3. Tổng mất 30 HP |
| TC2.3 | Tag Removal | Remove Tag `State.Debuff` | Toàn bộ Effects có tag này bị xóa ngay lập tức |
| TC2.4 | Infinite Effect | Apply Buff Infinite | Buff tồn tại vĩnh viễn cho đến khi nhấn Dispel |

---

### PHASE 3: Stacking Logic
*Mục tiêu: Xác minh cơ chế cộng dồn (Max Stacks, Scaling).*

- **Task 3.1**: Implement Max Stack check.
- **Task 3.2**: Implement Refresh Policy logic.

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC3.1 | Max Stack Limit | Apply Buff (+5%), Max 3 stacks, nhấn 5 lần | Speed tăng tối đa 15% (+15% total) |
| TC3.2 | Refresh Policy | Stack 1 sắp hết hạn, apply Stack 2 | Cả 2 stacks được reset thời gian về ban đầu |
| TC3.3 | Stack Math | Damage 10 per stack, apply 3 stacks | Mỗi chu kỳ gây 30 damage |

---

### PHASE 4: Tag System & Ability Activation
*Mục tiêu: Xác minh Blocking, Requirements và Cooldown.*

- **Task 4.1**: Implement `ActivationRequiredTags` logic.
- **Task 4.2**: Implement `BlockAbilitiesWithTags` logic.

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC4.1 | Tag Requirement | Cast "Berserk" khi HP > 50% (thiếu tag LowHealth) | Trả về lỗi "Requirements not met" |
| TC4.2 | State Blocking | Bị Stun (State.Stunned), nhấn dùng chiêu | Ability không kích hoạt được |
| TC4.3 | Cooldown Scale | Cooldown 10s, Rate=2.0 (200%) | Ability hồi xong chỉ sau 5s thực tế |
| TC4.4 | Cancel Ability | Activate skill B có tag Cancel.SkillA | Skill A đang cast bị ngắt ngay lập tức |

---

### PHASE 5: Advanced Scenarios & Cues
*Mục tiêu: Xác minh các tính năng mới xây dựng ở Phase 0.*

- **Task 5.1**: Implement ScalableFloat verification UI.
- **Task 5.2**: Test Gameplay Event Payload (Magnitude passing).

| ID | Test Case | Input | Expected Output |
|---|---|---|---|
| TC5.1 | Event Magnitude | Gửi Event `Heal` với Magnitude=50 | Ability nhận đúng 50 để hồi máu |
| TC5.2 | Cue Lifecycle | Buff giáp (Duration 10s) | VFX Khiên hiện ra lúc apply, biến mất sau 10s |
| TC5.3 | SetByCaller | Set magnitude qua tag `Data.Dmg` | Damage tính theo giá trị truyền vào thay vì data gốc |

---

### PHASE 6: GUI All-in-One Suite
*Mục tiêu: Đóng gói thành công cụ test hoàn chỉnh.*

- [ ] Tích hợp nút "Run All Tests" (Automation).
- [ ] Báo cáo kết quả bằng bảng màu (Xanh/Đỏ).
- [ ] Sandbox mode: Tự do chỉnh sửa Attributes/Tags/Effects của Player và Enemy.
