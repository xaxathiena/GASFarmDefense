using FD.Character;
using GAS;
using UnityEngine;
using System.Collections.Generic;

namespace FD.Ability
{
    /// <summary>
    /// Fire Area Ability - Periodically shoots fire at enemy position
    /// Every 10 seconds, creates a fire area at enemy's location
    /// Fire lasts 3 seconds and deals 20 damage per second to enemies inside
    /// Applies burning effect with gameplay tag
    /// </summary>
    [CreateAssetMenu(fileName = "FireAreaAbility", menuName = "FD/Abilities/Fire Area Ability")]
    public class FireAreaAbility : FDGameplayAbility
    {
        [Header("Fire Area Configuration")]
        [Tooltip("GameplayEffect for burning damage (20 damage per second)")]
        [SerializeField] private GameplayEffect burningEffect;
        
        [Tooltip("Fire area prefab (will be created via UnityMCP if not set)")]
        [SerializeField] private GameObject fireAreaPrefab;
        
        [Tooltip("Fire area duration in seconds")]
        [SerializeField] private float fireDuration = 3f;
        
        [Tooltip("Fire area radius")]
        [SerializeField] private float fireRadius = 2f;
        
        [Tooltip("Layer mask for enemy detection")]
        [SerializeField] private LayerMask enemyLayerMask = ~0;
        
        [Tooltip("Show debug gizmos")]
        [SerializeField] private bool showDebug = true;

        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            base.OnAbilityActivated(asc, spec);

            var owner = GetAbilityOwner(asc);
            if (owner == null)
            {
                Debug.LogWarning("[FireAreaAbility] No owner found");
                EndAbility(asc, spec);
                return;
            }

            if (burningEffect == null)
            {
                Debug.LogError("[FireAreaAbility] No burning effect assigned!");
                EndAbility(asc, spec);
                return;
            }

            // Get character component to access targets
            var character = owner.GetComponent<BaseCharacter>();
            if (character == null)
            {
                Debug.LogWarning($"[FireAreaAbility] {owner.name} doesn't have BaseCharacter component!");
                EndAbility(asc, spec);
                return;
            }

            // Get targets from character
            List<Transform> targets = character.GetTargets();
            if (targets == null || targets.Count == 0)
            {
                if (showDebug)
                {
                    Debug.Log($"[FireAreaAbility] No targets found for {owner.name}");
                }
                EndAbility(asc, spec);
                return;
            }

            // Use first target's position
            Transform target = targets[0];
            if (target == null)
            {
                EndAbility(asc, spec);
                return;
            }

            Vector3 firePosition = target.position;
            
            if (showDebug)
            {
                Debug.Log($"[FireAreaAbility] Creating fire area at {firePosition}");
            }

            // Create fire area at target position
            CreateFireArea(firePosition, asc, spec);
            
            EndAbility(asc, spec);
        }

        private void CreateFireArea(Vector3 position, AbilitySystemComponent sourceASC, GameplayAbilitySpec spec)
        {
            GameObject fireArea;
            
            // Use prefab if assigned, otherwise create a visual sphere
            if (fireAreaPrefab != null)
            {
                fireArea = Object.Instantiate(fireAreaPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create default fire area object with visual
                fireArea = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fireArea.name = "FireArea";
                fireArea.transform.position = position;
                fireArea.transform.localScale = Vector3.one * fireRadius * 2f;
                
                // Setup visual material
                var renderer = fireArea.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(1f, 0.3f, 0f, 0.5f); // Orange/red transparent
                    
                    // Enable transparency
                    mat.SetFloat("_Mode", 3); // Transparent mode
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    
                    // Add emission for glowing effect
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f, 1f));
                    
                    renderer.material = mat;
                }
            }
            
            // Setup fire area detector component
            var detector = fireArea.GetComponent<AuraDetector>();
            if (detector == null)
            {
                detector = fireArea.AddComponent<AuraDetector>();
            }
            
            // Initialize the detector
            detector.Initialize(
                fireRadius,
                fireDuration,
                burningEffect,
                sourceASC,
                enemyLayer: enemyLayerMask
            );
            
            if (showDebug)
            {
                Debug.Log($"[FireAreaAbility] Fire area created with radius {fireRadius}, duration {fireDuration}s");
            }
        }
    }
}
