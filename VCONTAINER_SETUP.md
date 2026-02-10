# VContainer Setup Guide - GASFarmDefense

## ✅ Installation Complete

VContainer đã được cài đặt và code đã được update để sử dụng đúng chuẩn VContainer!

## 📋 Setup Instructions

### 1. Create GameLifetimeScope in Scene

1. Tạo empty GameObject trong scene
2. Rename thành "GameLifetimeScope" hoặc "DI Container"
3. Add component: **GameLifetimeScope** (từ FD.DI namespace)
4. Enable "Log Registrations" để debug

### 2. Create Enemy Prefab

1. Create GameObject → 3D Object → Cube (hoặc model của bạn)
2. Add component: **EnemyView** (từ FD.Views namespace)
3. Optional: Add Animator, Renderer components
4. Save as Prefab trong Assets/Prefabs/Enemies/

### 3. Create Enemy Config

1. Right-click trong Project → Create → FD → Enemy Config
2. Configure stats:
   - Enemy ID: "Enemy_Basic"
   - Health: 1000
   - Move Speed: 3
   - Detection Range: 10
   - Attack Range: 2
3. Save as "EnemyConfig_Basic.asset"

### 4. Setup EnemySpawner

1. Create empty GameObject → Rename "EnemySpawner"
2. Add component: **EnemySpawner** (từ FD.Spawners namespace)
3. Configure inspector:
   - Enemy View Prefab: Assign prefab từ step 2
   - Default Config: Assign config từ step 3
   - Spawn Point: Create empty GameObject làm spawn position
   - Path Points: Create multiple empty GameObjects để làm waypoints
4. ✅ VContainer sẽ tự động inject dependencies!

### 5. Test Spawn

1. Play scene
2. Right-click EnemySpawner component trong Hierarchy
3. Chọn "Spawn Test Enemy"
4. Kiểm tra Console logs:
   ```
   [GameLifetimeScope] VContainer DI container configured successfully!
   [GameInitializer] Game started with VContainer DI!
   [EnemySpawner] Constructed with VContainer injection!
   [EnemySpawner] Spawned enemy 'Enemy_Basic' at (0, 0, 0)
   [GameInitializer] Enemy spawned at (0, 0, 0)
   ```

## 🔧 VContainer Configuration

### Current Registrations

**Services (Singleton):**
- `IGameplayEventBus` → `GameplayEventBus`
- `IEnemyRegistry` → `EnemyRegistry`
- `IEnemyMovementService` → `PathMovementService`
- `IEnemyAIService` → `BasicEnemyAI`

**Factories (Singleton):**
- `EnemyControllerFactory` - Creates EnemyController instances

**Entry Points:**
- `GameInitializer` - Runs on game start, sets up event listeners

**Scene MonoBehaviours:**
- `EnemySpawner` - Auto-injected by VContainer

### Dependency Graph

```
GameLifetimeScope (LifetimeScope)
    │
    ├─▶ GameplayEventBus (Singleton)
    ├─▶ EnemyRegistry (Singleton)
    ├─▶ PathMovementService (Singleton)
    ├─▶ BasicEnemyAI (Singleton)
    │
    ├─▶ EnemyControllerFactory (Singleton)
    │    └─▶ Injects: Movement, AI, Registry, EventBus
    │
    ├─▶ EnemySpawner (Scene MonoBehaviour)
    │    └─▶ Injects: EnemyControllerFactory
    │
    └─▶ GameInitializer (Entry Point)
         └─▶ Injects: EventBus

On Spawn:
    EnemySpawner
        └─▶ Creates EnemyView (prefab)
        └─▶ Factory.Create(view, data)
             └─▶ new EnemyController(view, data, services...)
```

## 🎯 Key Changes from Manual DI

### Before (Manual):
```csharp
// GameLifetimeScope had static properties
public static IGameplayEventBus EventBus => _eventBus;

// EnemySpawner used static access
var controller = new EnemyController(
    view, config,
    GameLifetimeScope.EventBus,  // ❌ Static
    // ...
);
```

### After (VContainer):
```csharp
// GameLifetimeScope extends LifetimeScope
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IGameplayEventBus, GameplayEventBus>(Lifetime.Singleton);
    }
}

// EnemySpawner receives factory via injection
[Inject]
public void Construct(EnemyControllerFactory enemyFactory)
{
    _enemyFactory = enemyFactory;  // ✅ Injected
}
```

## 🧪 Testing

### Unit Tests
Services đã testable với pure C#:

```csharp
[Test]
public void Movement_Should_Calculate_Correct_Position()
{
    var service = new PathMovementService();
    var state = new EnemyState { CurrentPosition = Vector3.zero };
    var config = new EnemyData { MoveSpeed = 5f };
    
    var result = service.CalculateNextPosition(state, config, 1f);
    
    Assert.AreEqual(Vector3.forward * 5f, result);
}
```

### Integration Tests
VContainer cung cấp test utilities:

```csharp
[UnityTest]
public IEnumerator Enemy_Should_Spawn_With_DI()
{
    var scope = CreateTestScope();
    var spawner = scope.Container.Resolve<EnemySpawner>();
    
    var controller = spawner.SpawnEnemy();
    
    Assert.NotNull(controller);
    yield return null;
}
```

## ⚠️ Common Issues

### Issue 1: "Factory is null"
**Cause:** VContainer chưa inject vào EnemySpawner

**Fix:**
1. Đảm bảo GameLifetimeScope có trong scene
2. Check `RegisterComponentInHierarchy<EnemySpawner>()` được gọi
3. EnemySpawner phải là child của LifetimeScope hoặc trong cùng scene

### Issue 2: "Services are null in controller"
**Cause:** Factory dependencies không được resolve

**Fix:**
1. Check tất cả services được register trong Configure()
2. Verify lifetime (Singleton vs Transient)

### Issue 3: "Controller not ticking"
**Cause:** Manual Update loop trong EnemySpawner

**Current:** EnemySpawner.Update() calls controller.Tick() manually

**Future optimization:** Register controllers as ITickable trong VContainer

## 🚀 Next Steps

### Phase 1: Test Current Implementation ✅
- Spawn enemies
- Verify DI working
- Check events firing

### Phase 2: Integrate with Existing Systems
- Connect EnemyController với AbilitySystemComponent
- Hook attribute changes vào event bus
- Replace old EnemyBase/EnemyManager

### Phase 3: Optimize with ITickable
```csharp
// Register controllers as ITickable
builder.Register<EnemyController>(Lifetime.Transient)
    .AsImplementedInterfaces(); // ITickable, IEnemy, IDisposable
```

### Phase 4: Advanced Features
- Tower system migration
- Ability system integration
- Performance optimization với Jobs

## 📚 VContainer Documentation

- [Official Docs](https://vcontainer.hadashikick.jp/)
- [Constructor Injection](https://vcontainer.hadashikick.jp/resolving/constructor-injection)
- [Scene Integration](https://vcontainer.hadashikick.jp/integrations/unitask-integration)
- [Testing](https://vcontainer.hadashikick.jp/diagnostics/testing)

## 🎓 Architecture Benefits

✅ **Dependency Injection:** No more static calls or FindObjectOfType  
✅ **Testability:** All services pure C#, easy to mock  
✅ **Flexibility:** Swap implementations at registration time  
✅ **Decoupling:** Event bus replaces direct references  
✅ **Scalability:** Add features without modifying existing code

---

**Status:** ✅ Ready to use!  
**Last Updated:** 10/02/2026  
**VContainer Version:** Latest from openupm
