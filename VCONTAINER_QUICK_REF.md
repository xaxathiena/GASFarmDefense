# VContainer Quick Reference - GASFarmDefense

## 🎯 Setup Checklist (5 phút)

- [ ] **1. GameLifetimeScope in Scene**
  - GameObject → Add Component → GameLifetimeScope (FD.DI)
  - Enable "Log Registrations"

- [ ] **2. Enemy Prefab**
  - Cube + EnemyView component
  - Save as Prefab

- [ ] **3. Enemy Config**
  - Create → FD → Enemy Config
  - Set stats

- [ ] **4. EnemySpawner**
  - GameObject + EnemySpawner component
  - Assign prefab, config, spawn point, path points

- [ ] **5. Test**
  - Play → Right-click EnemySpawner → "Spawn Test Enemy"

## 🔑 Key VContainer Patterns

### Service Registration
```csharp
// GameLifetimeScope.cs
protected override void Configure(IContainerBuilder builder)
{
    // Singleton service
    builder.Register<IMyService, MyService>(Lifetime.Singleton);
    
    // Transient (new instance mỗi lần)
    builder.Register<IMyService, MyService>(Lifetime.Transient);
    
    // Scoped (new instance per scope)
    builder.Register<IMyService, MyService>(Lifetime.Scoped);
}
```

### Constructor Injection
```csharp
public class MyClass
{
    private readonly IMyService _service;
    
    // VContainer tự động inject
    public MyClass(IMyService service)
    {
        _service = service;
    }
}
```

### Method Injection (cho MonoBehaviour)
```csharp
public class MyMonoBehaviour : MonoBehaviour
{
    private IMyService _service;
    
    [Inject]
    public void Construct(IMyService service)
    {
        _service = service;
    }
}
```

### Property Injection (không khuyến khích)
```csharp
public class MyClass
{
    [Inject]
    public IMyService Service { get; set; }
}
```

### Factory Pattern
```csharp
// Registration
builder.Register<MyFactory>(Lifetime.Singleton);

// Factory class
public class MyFactory
{
    private readonly IService _service;
    
    public MyFactory(IService service)
    {
        _service = service;
    }
    
    public MyObject Create(params...)
    {
        return new MyObject(..., _service);
    }
}
```

### Entry Points
```csharp
// IStartable - chạy khi container khởi động
public class GameInitializer : IStartable
{
    public void Start() { }
}

// ITickable - chạy mỗi frame
public class GameUpdater : ITickable
{
    public void Tick() { }
}

// IPostStartable - chạy sau tất cả IStartable
public class LateInitializer : IPostStartable
{
    public void PostStart() { }
}

// Registration
builder.RegisterEntryPoint<GameInitializer>();
```

### Scene MonoBehaviour Injection
```csharp
// Auto-inject vào MonoBehaviour đã có trong scene
builder.RegisterComponentInHierarchy<MyMonoBehaviour>();

// Inject vào instance cụ thể
builder.RegisterComponent(myMonoBehaviourInstance);
```

## 📦 Current Architecture

```
Services (Pure C#, Testable)
├── IGameplayEventBus → GameplayEventBus
├── IEnemyRegistry → EnemyRegistry
├── IEnemyMovementService → PathMovementService
└── IEnemyAIService → BasicEnemyAI

Factories
└── EnemyControllerFactory

Controllers (Runtime)
└── EnemyController (created by factory)

Views (MonoBehaviour)
└── EnemyView (minimal, no logic)

Data (Plain classes)
├── EnemyData (config)
└── EnemyState (runtime)
```

## 🔥 Common Patterns

### Spawning with Factory
```csharp
public class Spawner : MonoBehaviour
{
    private MyFactory _factory;
    
    [Inject]
    public void Construct(MyFactory factory)
    {
        _factory = factory;
    }
    
    public void Spawn()
    {
        var obj = _factory.Create(params...);
    }
}
```

### Event Bus Pattern
```csharp
// Publisher
_eventBus.Publish(new MyEvent(data));

// Subscriber
_eventBus.Subscribe<MyEvent>(OnMyEvent);

private void OnMyEvent(MyEvent e)
{
    // Handle event
}

// Cleanup
_eventBus.Unsubscribe<MyEvent>(OnMyEvent);
```

### Service Locator (Anti-pattern - AVOID)
```csharp
// ❌ BAD - Don't do this!
var service = ServiceLocator.Get<IMyService>();

// ✅ GOOD - Use injection
public MyClass(IMyService service) { }
```

## 🐛 Debugging

### Check if DI is working
```csharp
[Inject]
public void Construct(IMyService service)
{
    if (service == null)
        Debug.LogError("DI FAILED!");
    else
        Debug.Log("DI SUCCESS!");
}
```

### Verify registrations
Enable "Log Registrations" trong GameLifetimeScope inspector

### Check scene hierarchy
GameLifetimeScope phải là parent hoặc trong cùng scene với MonoBehaviours cần inject

## ⚡ Performance Tips

1. **Singleton cho stateless services:** Movement, AI, calculations
2. **Transient cho runtime objects:** Controllers, temporary data
3. **Avoid property injection:** Slower than constructor
4. **Cache resolved services:** Không resolve trong Update()
5. **Use factories:** Tốt hơn là Resolve() nhiều lần

## 🎓 Best Practices

✅ **DO:**
- Constructor injection cho dependencies
- Singleton cho stateless services
- Factory pattern cho runtime objects
- Interface-based design
- Clear separation: Data-Logic-View

❌ **DON'T:**
- Static service access
- FindObjectOfType trong loops
- Property injection khi không cần
- Circular dependencies
- Resolve() trong hot paths (Update, FixedUpdate)

## 📋 Migration Checklist

- [x] Install VContainer
- [x] Create GameLifetimeScope
- [x] Register services
- [x] Update EnemySpawner với injection
- [x] Test spawning
- [ ] Integrate với AbilitySystemComponent
- [ ] Replace old EnemyBase
- [ ] Migrate Tower system
- [ ] Write unit tests

---

**Quick Help:**
- Docs: https://vcontainer.hadashikick.jp/
- Issues: Check Console logs
- Test: Right-click component → Context menu
