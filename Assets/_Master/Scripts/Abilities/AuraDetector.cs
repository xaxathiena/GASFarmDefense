using GAS;
using UnityEngine;
using System.Collections.Generic;
using FD.Character;

namespace FD.Ability
{
    /// <summary>
    /// Detects enemies entering/exiting aura and applies/removes slow effect
    /// Handles effect stacking automatically through GAS system
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
        
        private SphereCollider triggerCollider;
        private GameObject visualSphere;
        private float timer;
        private bool isInitialized = false;

        /// <summary>
        /// Initialize the aura detector
        /// </summary>
        public void Initialize(float auraRadius, float auraDuration, GameplayEffect effect, 
                              AbilitySystemComponent owner, Transform follow = null)
        {
            radius = auraRadius;
            duration = auraDuration;
            effectToApply = effect;
            ownerASC = owner;
            followTarget = follow;
            timer = duration;
            
            SetupCollider();
            SetupVisuals();
            
            isInitialized = true;
            
            Debug.Log($"AuraDetector initialized - Radius: {radius}, Duration: {duration}");
        }

        private void SetupCollider()
        {
            // Create trigger collider for detection
            triggerCollider = gameObject.GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            
            triggerCollider.isTrigger = true;
            triggerCollider.radius = radius;
            
            // Add Rigidbody for trigger to work
            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
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
            if (duration > 0)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    DestroyAura();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized) return;

            // Enemy enters aura
            var character = other.GetComponent<BaseCharacter>();
            if (character == null) return;
            
            var targetASC = character.AbilitySystemComponent;
            if (targetASC == null || targetASC == ownerASC) return;
            
            // Apply slow effect
            ApplySlowEffect(targetASC);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isInitialized) return;

            // Enemy exits aura
            var character = other.GetComponent<BaseCharacter>();
            if (character == null) return;
            
            var targetASC = character.AbilitySystemComponent;
            if (targetASC == null) return;
            
            // Remove slow effect
            RemoveSlowEffect(targetASC);
        }

        private void ApplySlowEffect(AbilitySystemComponent targetASC)
        {
            if (effectToApply == null) return;
            
            // Check if already affected by this aura
            if (affectedTargets.ContainsKey(targetASC))
            {
                Debug.LogWarning($"Target {targetASC.gameObject.name} already affected by this aura");
                return;
            }

            // Apply effect and store handle
            var activeEffect = targetASC.ApplyGameplayEffectToSelf(effectToApply);
            #if UNITY_EDITOR
            if (activeEffect != null)
            {
                affectedTargets[targetASC] = activeEffect;
                
                // Debug info
                int stackCount = activeEffect.StackCount;
                Debug.Log($"✓ Applied slow to {targetASC.gameObject.name} (Stack: {stackCount})");
                
                // Log current move speed
                var attrSet = targetASC.AttributeSet;
                if (targetASC.AttributeSet  != null)
                {
                    float currentSpeed = attrSet.GetAttribute(EGameplayAttributeType.MoveSpeed).CurrentValue;
                    Debug.Log($"  → Current MoveSpeed: {currentSpeed:F2}");
                }
            }
            #endif
        }

        private void RemoveSlowEffect(AbilitySystemComponent targetASC)
        {
            if (!affectedTargets.ContainsKey(targetASC)) return;

            var activeEffect = affectedTargets[targetASC];
            if (activeEffect != null)
            {
                targetASC.RemoveGameplayEffect(activeEffect);
                Debug.Log($"✗ Removed slow from {targetASC.gameObject.name}");
                
                // Log restored speed
                var attrSet = targetASC.AttributeSet as FDAttributeSet;
                if (attrSet != null)
                {
                    float currentSpeed = attrSet.GetAttribute(EGameplayAttributeType.MoveSpeed).CurrentValue;
                    Debug.Log($"  → Restored MoveSpeed: {currentSpeed:F2}");
                }
            }

            affectedTargets.Remove(targetASC);
        }

        private void DestroyAura()
        {
            Debug.Log("Aura expired, removing all effects");
            
            // Remove all effects before destroying
            var targets = new List<AbilitySystemComponent>(affectedTargets.Keys);
            foreach (var target in targets)
            {
                RemoveSlowEffect(target);
            }
            
            affectedTargets.Clear();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Cleanup on unexpected destroy
            var targets = new List<AbilitySystemComponent>(affectedTargets.Keys);
            foreach (var target in targets)
            {
                if (target != null)
                {
                    var activeEffect = affectedTargets[target];
                    if (activeEffect != null)
                    {
                        target.RemoveGameplayEffect(activeEffect);
                    }
                }
            }
            affectedTargets.Clear();
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
            foreach (var target in affectedTargets.Keys)
            {
                if (target != null)
                {
                    Gizmos.DrawLine(transform.position, target.transform.position);
                }
            }
        }
    }
}
