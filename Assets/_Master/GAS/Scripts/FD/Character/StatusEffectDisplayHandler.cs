using UnityEngine;
using GAS;
using FD.UI;

namespace FD.Character
{
    /// <summary>
    /// Automatically creates and manages a StatusEffectDisplayManager for this character
    /// Attach this to any GameObject with an AbilitySystemComponent to show status effects
    /// </summary>
    [RequireComponent(typeof(AbilitySystemComponent))]
    public class StatusEffectDisplayHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas statusEffectCanvas;
        [SerializeField] private RectTransform iconContainerPrefab;
        
        [Header("Setup")]
        [SerializeField] private bool autoCreate = true;
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0);

        private StatusEffectDisplayManager displayManager;
        private AbilitySystemComponent asc;
        private RectTransform iconContainer;

        private void Start()
        {
            asc = GetComponent<AbilitySystemComponent>();

            if (autoCreate)
            {
                CreateDisplayManager();
            }
        }

        /// <summary>
        /// Create the display manager
        /// </summary>
        public void CreateDisplayManager()
        {
            if (displayManager != null)
                return;

            // Find or create canvas
            if (statusEffectCanvas == null)
            {
                statusEffectCanvas = FindFirstObjectByType<Canvas>();
                
                if (statusEffectCanvas == null)
                {
                    Debug.LogWarning($"[StatusEffectDisplayHandler] No Canvas found for {gameObject.name}. Cannot display status effects.");
                    return;
                }
            }

            // Create icon container
            if (iconContainerPrefab != null)
            {
                iconContainer = Instantiate(iconContainerPrefab, statusEffectCanvas.transform);
            }
            else
            {
                // Create default container
                var containerGO = new GameObject($"StatusEffects_{gameObject.name}");
                containerGO.transform.SetParent(statusEffectCanvas.transform, false);
                iconContainer = containerGO.AddComponent<RectTransform>();
                
                // Setup layout
                var horizontalLayout = containerGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                horizontalLayout.spacing = 5f;
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childControlHeight = false;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childForceExpandHeight = false;
                horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            }

            // Add display manager component
            displayManager = iconContainer.gameObject.AddComponent<StatusEffectDisplayManager>();
            
            // Initialize
            if (asc != null)
            {
                displayManager.Initialize(asc);
            }
        }

        /// <summary>
        /// Manually refresh the display
        /// </summary>
        public void RefreshDisplay()
        {
            if (displayManager != null)
            {
                displayManager.RefreshAllIcons();
            }
        }

        private void OnDestroy()
        {
            // Clean up container
            if (iconContainer != null)
            {
                Destroy(iconContainer.gameObject);
            }
        }
    }
}
