# Global Event System (Pub/Sub) Architecture Plan (R3 Integration)

Dựa trên hệ sinh thái Cysharp mà bạn đang xem xét, **R3** (tiền thân là UniRx) chính là "chân ái" và là mảnh ghép hoàn hảo nhất cho hệ thống Event (Pub/Sub) của dự án. Việc kết hợp R3 vào kiến trúc VContainer sẽ tạo ra một hệ thống xử lý sự kiện (Message Broker) cực kỳ hiện đại, chuyên nghiệp và có chuẩn mực công nghiệp.

## 1. Vì sao nên dùng R3 cho Event System?
*   **Reactive Messaging (Message Broker)**: Thay vì truyền Event bằng C# `Action` thuần, R3 cho phép trả về cấu trúc `Observable<T>`. Sức mạnh của nó nằm ở chỗ bạn có thể áp dụng các Operator để transform data ngay trên luồng stream (như `.Where()` lọc điều kiện, `.Delay()` chờ một chút mới xử lý, `.ThrottleFirst()` ngăn spam event).
*   **Zero Allocation & High Performance**: R3 là nỗ lực của Cysharp viết lại từ đầu chuẩn Reactive Extensions sang kiến trúc triệt tiêu allocation, hạn chế tối đa sinh rác (GC) - một thứ tối quan trọng khi làm game.
*   **Quản lý Lifecycle tự động**: Lỗi phổ biến nhất của Event C# là "quên Unsubscribe" dẫn tới rò rỉ bộ nhớ (Memory Leak) hoặc Null Reference. Với R3, bạn dễ dàng buộc tuổi thọ của luồng event vào 1 đối tượng/MonoBehaviour (ví dụ: `builder.AddTo(gameObject)` hoặc `.AddTo(cancellationToken)`).

## 2. Proposed Implementation Cấu trúc Module

Chúng ta sẽ xây dựng EventBus theo chuẩn **Message Broker** của R3.

### [NEW] `IEvent.cs`
Marker Interface cho các Event. Mọi thông tin cần truyền trải nên được gói gọn vào một struct.
```csharp
public interface IEvent { }
```

### [NEW] `IEventBus.cs`
Interface của hệ thống. Nó sẽ cung cấp một kênh phát (Publish) và một kênh nhận (Receive trả về Observable).
```csharp
using R3;

namespace Abel.GASFarmDefense.Core.EventBus
{
    public interface IEventBus
    {
        // Đăng ký nhận luồng sự kiện T. Trả về Observable để kết nối Rx Operators.
        Observable<T> Receive<T>() where T : IEvent;
        
        // Phát tán sự kiện T.
        void Publish<T>(T @event) where T : IEvent;
    }
}
```

### [NEW] `EventBus.cs`
Lớp Core implement IEventBus bằng cách cache và trả về các kênh `Subject<T>` (Kênh truyền tín hiệu của R3). Lớp này sẽ được inject bằng VContainer dưới dạng Singleton `[RegisterSingleton(typeof(IEventBus))]`.
```csharp
using System;
using System.Collections.Generic;
using R3;

namespace Abel.GASFarmDefense.Core.EventBus
{
    public class EventBus : IEventBus, IDisposable
    {
        // Lưu trữ các luồng Subject theo type của Sự kiện.
        private readonly Dictionary<Type, object> _subjects = new Dictionary<Type, object>();

        public Observable<T> Receive<T>() where T : IEvent
        {
            var type = typeof(T);
            if (!_subjects.TryGetValue(type, out var subject))
            {
                subject = new Subject<T>();
                _subjects[type] = subject;
            }
            return ((Subject<T>)subject).AsObservable();
        }

        public void Publish<T>(T @event) where T : IEvent
        {
            var type = typeof(T);
            if (_subjects.TryGetValue(type, out var subject))
            {
                ((Subject<T>)subject).OnNext(@event);
            }
        }

        // Cleanup an toàn khi đổi Scene hoặc tắt game.
        public void Dispose()
        {
            foreach (var subject in _subjects.Values)
            {
                if (subject is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _subjects.Clear();
        }
    }
}
```

## 3. Cách sử dụng mẫu sau khi Deploy

**Định nghĩa Event (Dùng cấu trúc struct để trành GC Alloc):**
```csharp
public struct EnemyDiedEvent : IEvent
{
    public int EnemyInstanceID;
    public Vector3 DeathPosition;
}
```

**Subscriber (Kẻ nghe Event) - Xử lý cực kỳ dọn gàng với R3:**
```csharp
using R3;

public class SoundManager : IInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public SoundManager(IEventBus eventBus) { _eventBus = eventBus; }

    public void Initialize() 
    {
        _eventBus.Receive<EnemyDiedEvent>()
                 // Ví dụ quyền năng của R3: Chỉ phát âm nếu quái chết ở nhánh path số 1 (Z > 0)
                 .Where(evt => evt.DeathPosition.z > 0)
                 // Đăng ký nhận luồng
                 .Subscribe(evt => PlaySoundAt("Enemy_Die", evt.DeathPosition))
                 // Quản lý lifetime dễ dàng, EventBus tự ngắt kết nối khi SoundManager Dispose
                 .AddTo(_disposables);
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
```

**Publisher (Kẻ phát Event) - Ví dụ khi EnemyManager báo quái chết:**
```csharp
private void KillEnemy(Enemy enemy, bool reachedBase, bool notify)
{
    // ...
    // Hệ thống hoàn toàn decoupling. EnemyManager không cần biết SoundManager có sống hay không
    _eventBus.Publish(new EnemyDiedEvent 
    { 
        EnemyInstanceID = enemy.InstanceID,
        DeathPosition = enemy.Position
    });
}
```

Việc tích hợp R3 làm nền tảng Message Broker đáp ứng hoàn toàn yêu cầu cho một game lớn, dễ theo dõi, tối ưu mạnh mẽ rác GC và có thể handle những flow event bất đồng bộ phức tạp.
