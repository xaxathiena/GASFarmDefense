# Performance Test Scene - Hướng dẫn sử dụng

## Tổng quan
Scene này được tạo để test hiệu suất của hệ thống Tower Defense với nhiều towers và enemies.

## Các thành phần đã tạo

### 1. Enemy Prefabs (3 loại mới)
Ngoài Enemy01 ban đầu, đã tạo thêm:

- **FastEnemy** (màu xanh lá): 
  - Tốc độ di chuyển: 5 (nhanh)
  - Kích thước: nhỏ (0.5x)
  
- **TankEnemy** (màu đỏ):
  - Tốc độ di chuyển: 1.5 (chậm)
  - Kích thước: lớn (1.2x)
  - Phù hợp cho enemy có nhiều HP
  
- **FlyingEnemy** (màu xanh dương):
  - Tốc độ di chuyển: 3.5 (trung bình)
  - Vị trí: bay cao hơn (y=2)
  - Kích thước: trung bình (0.6x)

### 2. Tower Prefabs (3 loại)

- **BasicTower** (màu xanh dương):
  - Range: 8 units
  - Max Targets: 1
  - Phù hợp cho DPS cơ bản
  
- **SniperTower** (màu vàng):
  - Range: 15 units (xa nhất)
  - Max Targets: 1
  - Phù hợp cho tower có damage cao
  
- **AOETower** (màu tím):
  - Range: 10 units
  - Max Targets: 5 (đánh nhiều mục tiêu)
  - Phù hợp cho AoE abilities

### 3. PerformanceTestManager

Script này quản lý việc spawn towers tự động trong scene.

#### Các tham số chính:
- **Number of Towers**: Số lượng tower sẽ spawn (mặc định: 15)
- **Tower Prefabs**: Danh sách các tower prefab có thể spawn
- **Randomize Tower Types**: Chọn ngẫu nhiên loại tower
- **Spawn Towers On Start**: Tự động spawn khi chạy game
- **Path Points**: Reference đến các waypoints (để spawn gần đường đi)
- **Offset From Path**: Khoảng cách từ đường đi (mặc định: 3 units)

#### Performance Metrics:
- Hiển thị FPS real-time
- Số lượng towers đang active
- Số lượng enemies đang active

### 4. Scene Setup

#### Path Points (Đường đi của enemy):
- Path_Start → Path_01 → Path_02 → Path_03 → Path_End
- Tạo thành đường hình "Z"

#### EnemyWaveController:
- Đã cấu hình sẵn 1 wave với tất cả 4 loại enemy
- Mỗi loại spawn 5 con
- Spawn interval: 0.3s
- Auto start khi play

## Cách sử dụng

### Bước 1: Mở scene
Mở scene `Assets/TestPerformance.unity` trong Unity

### Bước 2: Cấu hình (nếu cần)
1. Chọn GameObject `PerformanceTestManager`
2. Điều chỉnh `Number of Towers` để tăng/giảm số tower
3. Điều chỉnh `Offset From Path` để thay đổi vị trí spawn

### Bước 3: Chạy test
1. Nhấn Play
2. Towers sẽ tự động spawn gần đường đi
3. Enemies sẽ spawn và đi theo path
4. Xem FPS và performance metrics ở góc trên bên trái màn hình

### Bước 4: Điều chỉnh
- Để spawn lại towers, có thể gọi `PerformanceTestManager.SpawnTowers()` từ code
- Để clear towers, gọi `PerformanceTestManager.ClearTowers()`
- Hoặc sử dụng Menu: `FD/Setup Performance Test Scene` để reset scene

## Tips để test performance

### Test với nhiều enemies:
- Trong `EnemyWaveController`, tăng số `count` của mỗi enemy type
- Giảm `spawnInterval` để spawn nhanh hơn
- Thêm nhiều waves

### Test với nhiều towers:
- Tăng `numberOfTowers` trong PerformanceTestManager
- Có thể đến 50-100 towers

### Test các kịch bản khác nhau:
1. **Spam test**: Nhiều enemies + nhiều towers cùng lúc
2. **Long fight**: Ít towers nhưng enemies spawn liên tục
3. **Boss test**: 1 enemy với HP rất cao, nhiều towers tập trung bắn

## Tùy chỉnh thêm

### Thêm abilities cho towers:
1. Tạo GameplayAbility mới trong `Assets/_Master/GAS/SO/Abilities`
2. Assign vào `abilities` list của tower prefab
3. Cấu hình level và passive/active

### Thêm thuộc tính cho enemies:
- Mở prefab trong Prefab Mode
- Chỉnh sửa component FDEnemyBase
- Thay đổi moveSpeed, health, armor, etc.

### Thêm visual effects:
- Assign materials khác cho prefabs
- Thêm particle systems
- Thêm trails

## Troubleshooting

### Towers không bắn:
- Kiểm tra xem towers có abilities không
- Kiểm tra LayerMask của tower có include enemy layer không
- Kiểm tra targetRange có đủ lớn không

### Enemies không di chuyển:
- Kiểm tra PathPoints có được assign đúng không
- Kiểm tra thứ tự của path points (phải theo tên)
- Kiểm tra moveSpeed > 0

### FPS thấp:
- Giảm số lượng towers/enemies
- Tắt shadows/post-processing
- Optimize abilities (giảm particles, effects)
