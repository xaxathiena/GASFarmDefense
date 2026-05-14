using UnityEngine;
using Abel.GAS;
using Abel.GAS.Cues;

namespace Abel.GAS.Samples.OrbCombat
{
    /// <summary>
    /// A simple test implementation of Gameplay Cues for the Orb Combat sample.
    /// </summary>
    public class OrbCueTest : MonoBehaviour, IGameplayCueNotify
    {
        [SerializeField] private GameplayTag cueTag;
        [SerializeField] private GameObject visualEffectPrefab;
        
        private GameObject _activeEffect;

        public GameplayTag CueTag => cueTag;

        public void Initialize(GameplayTag tag, GameObject prefab = null)
        {
            cueTag = tag;
            visualEffectPrefab = prefab;
            
            // Re-register if initialized late
            if (GameplayCueManager.Instance != null)
            {
                GameplayCueManager.Instance.RegisterNotifier(this);
            }
        }

        private void OnEnable()
        {
            // Register with the manager
            if (GameplayCueManager.Instance != null)
            {
                GameplayCueManager.Instance.RegisterNotifier(this);
            }
        }

        private void OnDisable()
        {
            // Unregister
            if (GameplayCueManager.Instance != null)
            {
                GameplayCueManager.Instance.UnregisterNotifier(this);
            }
            
            Cleanup();
        }

        public void HandleCue(EGameplayCueEvent cueEvent, GameplayCueParameters parameters)
        {
            Debug.Log($"[OrbCueTest] Handled Cue {cueTag} Event: {cueEvent} on {gameObject.name}");

            switch (cueEvent)
            {
                case EGameplayCueEvent.OnActive:
                    SpawnEffect();
                    break;
                    
                case EGameplayCueEvent.OnRemove:
                    Cleanup();
                    break;
                    
                case EGameplayCueEvent.Execute:
                    // One-shot at location
                    SpawnBurst(parameters.Location);
                    break;
            }
        }

        private void SpawnEffect()
        {
            if (_activeEffect != null) return;
            
            if (visualEffectPrefab != null)
            {
                _activeEffect = Instantiate(visualEffectPrefab, transform);
                _activeEffect.transform.localPosition = Vector3.zero;
            }
            else
            {
                // Fallback: Create a primitive
                _activeEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _activeEffect.transform.SetParent(transform);
                _activeEffect.transform.localPosition = new Vector3(0, 1f, 0); // Above head
                _activeEffect.transform.localScale = Vector3.one * 0.5f;
                
                var renderer = _activeEffect.GetComponent<Renderer>();
                renderer.material.color = Color.cyan;
                // Make it look a bit like a shield
                Destroy(_activeEffect.GetComponent<Collider>());
            }
        }

        private void SpawnBurst(Vector3 location)
        {
            GameObject burst;
            if (visualEffectPrefab != null)
            {
                burst = Instantiate(visualEffectPrefab, location, Quaternion.identity);
            }
            else
            {
                burst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                burst.transform.position = location;
                burst.transform.localScale = Vector3.one * 0.3f;
                burst.GetComponent<Renderer>().material.color = Color.red;
                Destroy(burst.GetComponent<Collider>());
            }
            
            Destroy(burst, 1.5f);
        }

        private void Cleanup()
        {
            if (_activeEffect != null)
            {
                Destroy(_activeEffect);
                _activeEffect = null;
            }
        }
    }
}
