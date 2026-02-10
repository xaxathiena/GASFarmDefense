using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FD.DI
{
    /// <summary>
    /// Simple debug script để verify VContainer có hoạt động không
    /// </summary>
    public class VContainerDebugger : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logOnAwake = true;
        [SerializeField] private bool logOnStart = true;
        
        private void Awake()
        {
            if (logOnAwake)
            {
                Debug.Log("[VContainerDebugger] Awake called!");
                
                // Check if LifetimeScope exists
                var scope = FindAnyObjectByType<LifetimeScope>();
                if (scope == null)
                {
                    Debug.LogError("[VContainerDebugger] ❌ NO LifetimeScope found in scene!");
                }
                else
                {
                    Debug.Log($"[VContainerDebugger] ✅ Found LifetimeScope: {scope.name}");
                    Debug.Log($"[VContainerDebugger] Container exists: {scope.Container != null}");
                }
            }
        }
        
        private void Start()
        {
            if (logOnStart)
            {
                Debug.Log("[VContainerDebugger] Start called!");
                
                var scope = FindAnyObjectByType<LifetimeScope>();
                if (scope != null)
                {
                    Debug.Log($"[VContainerDebugger] Container in Start: {scope.Container != null}");
                    
                    if (scope.Container != null)
                    {
                        // Try to resolve a service
                        try
                        {
                            var eventBus = scope.Container.Resolve<FD.Events.IGameplayEventBus>();
                            if (eventBus != null)
                            {
                                Debug.Log("[VContainerDebugger] ✅ Successfully resolved IGameplayEventBus!");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[VContainerDebugger] ❌ Failed to resolve service: {e.Message}");
                        }
                    }
                }
            }
        }
        
        [ContextMenu("Test VContainer")]
        public void TestVContainer()
        {
            Debug.Log("=== VContainer Test ===");
            
            var scopes = FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None);
            Debug.Log($"Found {scopes.Length} LifetimeScope(s)");
            
            foreach (var scope in scopes)
            {
                Debug.Log($"- {scope.name} (Container: {scope.Container != null})");
            }
        }
    }
}
