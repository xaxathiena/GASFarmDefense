using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FD.Spawners
{
    /// <summary>
    /// Helper component để inject dependencies vào EnemySpawner
    /// Attach component này vào cùng GameObject với EnemySpawner
    /// </summary>
    [RequireComponent(typeof(EnemySpawner))]
    public class EnemySpawnerInjector : MonoBehaviour
    {
        private EnemySpawner _spawner;
        
        [Inject]
        public void Construct(IObjectResolver container)
        {
            _spawner = GetComponent<EnemySpawner>();
            if (_spawner != null)
            {
                // Inject dependencies vào spawner
                container.Inject(_spawner);
                Debug.Log("[EnemySpawnerInjector] Injected dependencies into EnemySpawner");
            }
            else
            {
                Debug.LogError("[EnemySpawnerInjector] EnemySpawner component not found!");
            }
        }
    }
}
