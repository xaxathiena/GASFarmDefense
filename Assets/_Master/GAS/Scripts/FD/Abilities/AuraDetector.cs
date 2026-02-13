using GAS;
using UnityEngine;
using System.Collections.Generic;
using FD.Character;

namespace FD.Ability
{
    /// <summary>
    /// Detects enemies entering/exiting aura and applies/removes slow effect
    /// Handles effect stacking automatically through GAS system
    /// Uses distance-based detection via EnemyManager (no Physics)
    /// </summary>
    public class AuraDetector : MonoBehaviour
    {
        private float radius;
        private float duration;
        private GameplayEffect effectToApply;
        private AbilitySystemComponent ownerASC;
        private Transform followTarget;

        // Track affected targets and their effect handles
        private Dictionary<AbilitySystemComponent, ActiveGameplayEffect> affectedTargets
            = new Dictionary<AbilitySystemComponent, ActiveGameplayEffect>();

        // Track enemies currently in range for enter/exit detection
        private HashSet<Transform> enemiesInRange = new HashSet<Transform>();
        
        // Reusable buffers to avoid allocations
        private HashSet<Transform> currentEnemiesSet = new HashSet<Transform>();
        private List<Transform> exitedEnemiesBuffer = new List<Transform>();

        private GameObject visualSphere;
        private float timer;
        private bool isInitialized = false;
        private LayerMask enemyLayer;
        private bool removeAuraAfterDuration = true;

        [Header("Performance")]
        [SerializeField] private float checkInterval = 0.1f; // Check 10 times per second
        private float nextCheckTime;

        /// <summary>
        /// Initialize the aura detector
        /// </summary>
        public void Initialize(float auraRadius, float auraDuration, GameplayEffect effect,
                              AbilitySystemComponent owner, LayerMask enemyLayer = default, Transform follow = null)
        {
            radius = auraRadius;
            duration = auraDuration;
            effectToApply = effect;
            ownerASC = owner;
            followTarget = follow;
            removeAuraAfterDuration = auraDuration > 0;
            timer = duration;
            this.enemyLayer = enemyLayer;
            SetupVisuals();

            isInitialized = true;
            nextCheckTime = Time.time;

#if UNITY_EDITOR
            Debug.Log($"AuraDetector initialized - Radius: {radius}, Duration: {duration}");
#endif
        }

        private void SetupVisuals()
        {
            // Create visual indicator sphere
            visualSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualSphere.name = "AuraVisual";
            visualSphere.transform.SetParent(transform);
            visualSphere.transform.localPosition = Vector3.zero;
            visualSphere.transform.localScale = Vector3.one * radius * 2;

            // Make it transparent
            var renderer = visualSphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0, 1f, 0.3f); // Purple transparent

                // Enable transparency
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                renderer.material = mat;
            }

            // Remove collider from visual sphere
            Destroy(visualSphere.GetComponent<Collider>());
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Follow target if set
            if (followTarget != null)
            {
                transform.position = followTarget.position;
            }

            // Countdown duration (0 = infinite)
            if (removeAuraAfterDuration)
            {
                if (duration > 0)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0)
                    {
                        DestroyAura();
                    }
                }
            }

            // Check for enemies in range periodically (no Physics)
            if (Time.time >= nextCheckTime)
            {
                CheckEnemiesInRange();
                nextCheckTime = Time.time + checkInterval;
            }
        }

        /// <summary>
        /// Check enemies in range using EnemyManager (distance-based, no Physics)
        /// </summary>
        private void CheckEnemiesInRange()
        {
            // // Get current enemies in range from EnemyManager
            // var currentEnemies = EnemyManager.GetEnemiesInRange(transform.position, radius, enemyLayer);
            
            // // Reuse HashSet to avoid allocation
            // currentEnemiesSet.Clear();
            // for (int i = 0; i < currentEnemies.Count; i++)
            // {
            //     if (currentEnemies[i] != null)
            //     {
            //         currentEnemiesSet.Add(currentEnemies[i]);
            //     }
            // }

            // // Find enemies that entered range (new enemies)
            // foreach (var enemy in currentEnemiesSet)
            // {
            //     if (!enemiesInRange.Contains(enemy))
            //     {
            //         // Enemy just entered range
            //         OnEnemyEnter(enemy);
            //     }
            // }

            // // Find enemies that exited range (missing enemies)
            // exitedEnemiesBuffer.Clear();
            // foreach (var enemy in enemiesInRange)
            // {
            //     if (enemy == null || !currentEnemiesSet.Contains(enemy))
            //     {
            //         exitedEnemiesBuffer.Add(enemy);
            //     }
            // }

            // for (int i = 0; i < exitedEnemiesBuffer.Count; i++)
            // {
            //     OnEnemyExit(exitedEnemiesBuffer[i]);
            // }

            // // Swap sets to avoid allocation (just clear and refill)
            // enemiesInRange.Clear();
            // foreach (var enemy in currentEnemiesSet)
            // {
            //     enemiesInRange.Add(enemy);
            // }
        }

        private void OnEnemyEnter(Transform enemyTransform)
        {
            var character = enemyTransform.GetComponent<BaseCharacter>();
            if (character == null) return;

            var targetASC = character.AbilitySystemComponent;
            if (targetASC == null || targetASC == ownerASC) return;

            // Apply effect
            ApplyEffect(targetASC);
        }

        private void OnEnemyExit(Transform enemyTransform)
        {
            if (enemyTransform == null) return;

            var character = enemyTransform.GetComponent<BaseCharacter>();
            if (character == null) return;

            var targetASC = character.AbilitySystemComponent;
            if (targetASC == null) return;

            // Remove effect
            RemoveEffect(targetASC);
        }

        private void ApplyEffect(AbilitySystemComponent targetASC)
        {
            if (effectToApply == null) return;

            // Check if already affected by this aura
            if (affectedTargets.ContainsKey(targetASC))
            {
                return;
            }

            // Apply effect and store handle
            var activeEffect = targetASC.ApplyGameplayEffectToSelf(effectToApply, ownerASC);
            if (activeEffect != null)
            {
                affectedTargets[targetASC] = activeEffect;
            }
        }

        private void RemoveEffect(AbilitySystemComponent targetASC)
        {
            if (!affectedTargets.ContainsKey(targetASC)) return;

            var activeEffect = affectedTargets[targetASC];
            if (activeEffect != null)
            {
                targetASC.RemoveGameplayEffect(activeEffect);
            }

            affectedTargets.Remove(targetASC);
        }

        private void DestroyAura()
        {
            // Remove all effects before destroying
            var targets = new List<AbilitySystemComponent>(affectedTargets.Keys);
            foreach (var target in targets)
            {
                RemoveEffect(target);
            }

            affectedTargets.Clear();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Cleanup on unexpected destroy
            if (affectedTargets.Count > 0)
            {
                var targets = new List<AbilitySystemComponent>(affectedTargets.Keys);
                foreach (var target in targets)
                {
                    if (target != null && affectedTargets.TryGetValue(target, out var activeEffect))
                    {
                        if (activeEffect != null)
                        {
                            target.RemoveGameplayEffect(activeEffect);
                        }
                    }
                }
                affectedTargets.Clear();
            }
        }

        // Debug visualization in Scene view
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, isInitialized ? radius : 5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, isInitialized ? radius : 5f);

            // Draw affected targets
            Gizmos.color = Color.red;
            foreach (var kvp in affectedTargets)
            {
                if (kvp.Key != null)
                {
                    Gizmos.DrawLine(transform.position, kvp.Key.GetOwner().position);
                }
            }
        }
    }
}
