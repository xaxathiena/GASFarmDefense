using System.Collections.Generic;
using UnityEngine;
using GAS;

namespace FD.UI
{
    /// <summary>
    /// Manages displaying status effect icons above characters in world space
    /// </summary>
    public class StatusEffectDisplayManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private StatusEffectIcon iconPrefab;

        [Header("Layout")]
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private float iconSpacing = 40f;
        [SerializeField] private float iconSize = 36f;
        [SerializeField] private int maxVisibleIcons = 6;

        [Header("Position")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0);
        [SerializeField] private Camera worldCamera;

        [Header("Effect Icons")]
        [SerializeField] private StatusEffectIconDatabase iconDatabase;

        private AbilitySystemComponent targetASC;
        private Dictionary<ActiveGameplayEffect, StatusEffectIcon> activeIcons = new Dictionary<ActiveGameplayEffect, StatusEffectIcon>();
        private Canvas cachedCanvas;
        private bool isInitialized;

        private void Awake()
        {
            if (iconContainer != null)
            {
                cachedCanvas = iconContainer.GetComponentInParent<Canvas>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        /// <summary>
        /// Initialize with target AbilitySystemComponent
        /// </summary>
        public void Initialize(AbilitySystemComponent asc)
        {
            if (asc == null)
                return;

            targetASC = asc;
            isInitialized = true;

            // Subscribe to effect events
            // Note: You may need to add these events to AbilitySystemComponent
            RefreshAllIcons();
        }

        private void Update()
        {
            if (!isInitialized || targetASC == null)
                return;

            // Update position to follow target
            UpdateWorldPosition();

            // Update all icon displays
            foreach (var kvp in activeIcons)
            {
                if (kvp.Value != null && kvp.Value.IsInitialized())
                {
                    kvp.Value.UpdateDisplay();
                }
            }

            // Check for changes in active effects
            CheckForEffectChanges();
        }

        /// <summary>
        /// Update container position to follow world target
        /// </summary>
        private void UpdateWorldPosition()
        {
            if (targetASC == null || iconContainer == null || worldCamera == null)
                return;

            Vector3 worldPos = targetASC.GetOwner().position + worldOffset;
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

            // Check if position is in front of camera
            if (screenPos.z < 0f)
            {
                iconContainer.gameObject.SetActive(false);
                return;
            }

            iconContainer.gameObject.SetActive(true);

            // Convert to canvas position
            if (cachedCanvas != null)
            {
                Camera canvasCamera = cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    iconContainer.parent as RectTransform,
                    screenPos,
                    canvasCamera,
                    out Vector2 localPoint))
                {
                    iconContainer.anchoredPosition = localPoint;
                }
            }
        }

        /// <summary>
        /// Check for changes in active effects and update icons
        /// </summary>
        private void CheckForEffectChanges()
        {
            if (targetASC == null)
                return;

            var currentEffects = targetASC.GetActiveGameplayEffects();
            if (currentEffects == null)
                return;

            // Find effects that were removed
            var effectsToRemove = new List<ActiveGameplayEffect>();
            foreach (var kvp in activeIcons)
            {
                if (!currentEffects.Contains(kvp.Key))
                {
                    effectsToRemove.Add(kvp.Key);
                }
            }

            // Remove old icons
            foreach (var effect in effectsToRemove)
            {
                RemoveIcon(effect);
            }

            // Add new effects
            foreach (var effect in currentEffects)
            {
                if (effect != null && effect.Effect != null && !activeIcons.ContainsKey(effect))
                {
                    // Only show effects with granted tags (visible status effects)
                    if (effect.Effect.grantedTags != null && effect.Effect.grantedTags.Length > 0)
                    {
                        AddIcon(effect);
                    }
                }
            }

            // Update layout
            UpdateIconLayout();
        }

        /// <summary>
        /// Refresh all icons from scratch
        /// </summary>
        public void RefreshAllIcons()
        {
            // Clear all existing icons
            foreach (var icon in activeIcons.Values)
            {
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            activeIcons.Clear();

            if (targetASC == null)
                return;

            // Add icons for all active effects
            var currentEffects = targetASC.GetActiveGameplayEffects();
            if (currentEffects != null)
            {
                foreach (var effect in currentEffects)
                {
                    if (effect != null && effect.Effect != null)
                    {
                        // Only show effects with granted tags
                        if (effect.Effect.grantedTags != null && effect.Effect.grantedTags.Length > 0)
                        {
                            AddIcon(effect);
                        }
                    }
                }
            }

            UpdateIconLayout();
        }

        /// <summary>
        /// Add icon for an effect
        /// </summary>
        private void AddIcon(ActiveGameplayEffect effect)
        {
            if (iconPrefab == null || iconContainer == null || effect == null)
                return;

            // Check if we've reached max icons
            if (activeIcons.Count >= maxVisibleIcons)
                return;

            // Get icon sprite from database
            Sprite iconSprite = GetIconForEffect(effect);

            // Create icon
            var icon = Instantiate(iconPrefab, iconContainer);
            icon.Initialize(effect, iconSprite);

            activeIcons[effect] = icon;
        }

        /// <summary>
        /// Remove icon for an effect
        /// </summary>
        private void RemoveIcon(ActiveGameplayEffect effect)
        {
            if (activeIcons.TryGetValue(effect, out var icon))
            {
                if (icon != null)
                {
                    icon.FadeOut();
                }
                activeIcons.Remove(effect);
            }
        }

        /// <summary>
        /// Update the layout of all icons
        /// </summary>
        private void UpdateIconLayout()
        {
            int index = 0;
            foreach (var icon in activeIcons.Values)
            {
                if (icon != null)
                {
                    float xPos = index * (iconSize + iconSpacing);
                    icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, 0);
                    index++;
                }
            }
        }

        /// <summary>
        /// Get icon sprite for an effect
        /// </summary>
        private Sprite GetIconForEffect(ActiveGameplayEffect effect)
        {
            if (effect == null || effect.Effect == null)
                return null;

            // Try to get from database
            if (iconDatabase != null)
            {
                // Use the first granted tag to find icon
                if (effect.Effect.grantedTags != null && effect.Effect.grantedTags.Length > 0)
                {
                    return iconDatabase.GetIcon(effect.Effect.grantedTags[0]);
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            // Clean up
            foreach (var icon in activeIcons.Values)
            {
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            activeIcons.Clear();
        }
    }
}
