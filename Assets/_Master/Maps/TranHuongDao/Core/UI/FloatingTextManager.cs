using UnityEngine;
using UnityEngine.Pool;

using VContainer.Unity;

namespace Abel.TranHuongDao.Core.UI
{
    public class FloatingTextManager : IInitializable, System.IDisposable
    {
        private FloatingText floatingTextPrefab;
        private int defaultPoolSize = 20;
        private int maxPoolSize = 100;

        private ObjectPool<FloatingText> _pool;
        private GameObject _rootContainer;

        public void Initialize()
        {
            // Create a parent object to hold all pooled texts to keep the hierarchy clean
            _rootContainer = new GameObject("[FloatingTextManager_Pool]");
            Object.DontDestroyOnLoad(_rootContainer);

            // Attempt to load from resources
            floatingTextPrefab = Resources.Load<FloatingText>("Prefabs/FloatingText");

            if (floatingTextPrefab != null)
            {
                _pool = new ObjectPool<FloatingText>(
                    createFunc: CreateFloatingText,
                    actionOnGet: OnGetFloatingText,
                    actionOnRelease: OnReleaseFloatingText,
                    actionOnDestroy: OnDestroyFloatingText,
                    collectionCheck: false,
                    defaultCapacity: defaultPoolSize,
                    maxSize: maxPoolSize
                );
            }
            else
            {
                Debug.LogError("[FloatingTextManager] FloatingText prefab is missing! Please make sure it's located at Resources/Prefabs/FloatingText.prefab");
            }
        }

        public void Dispose()
        {
            if (_pool != null)
            {
                _pool.Dispose();
            }
            if (_rootContainer != null)
            {
                Object.Destroy(_rootContainer);
            }
        }

        // Object Pool Callbacks
        private FloatingText CreateFloatingText()
        {
            var inst = Object.Instantiate(floatingTextPrefab, _rootContainer.transform);
            // Just incase it defaults to an awkward rotation
            inst.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            inst.gameObject.SetActive(false);
            return inst;
        }

        private void OnGetFloatingText(FloatingText text)
        {
            text.gameObject.SetActive(true);
        }

        private void OnReleaseFloatingText(FloatingText text)
        {
            text.gameObject.SetActive(false);
        }

        private void OnDestroyFloatingText(FloatingText text)
        {
            if (text != null) Object.Destroy(text.gameObject);
        }

        // Public API
        /// <summary>
        /// Spawns a floating text at the given world position.
        /// </summary>
        public void ShowText(Vector3 position, string text, Color color, float scale = 1f)
        {
            if (_pool == null) return;

            FloatingText ft = _pool.Get();
            ft.transform.position = position;

            // Generate a random outward/upward direction on the XZ plane for variety
            float randomX = Random.Range(-0.8f, 0.8f);
            float randomZ = Random.Range(1.2f, 2.0f); // Move 'up' along the 45 degree orthographic view
            Vector3 direction = new Vector3(randomX, 0f, randomZ);

            ft.Setup(_pool, text, color, scale, direction, 1.2f);
        }
    }
}
