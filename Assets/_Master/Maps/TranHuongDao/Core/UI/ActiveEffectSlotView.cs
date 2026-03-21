using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Drives a single active-effect slot in the selection UI.
    /// Bind a live ActiveGameplayEffect to it, then call Tick() every frame to update the duration bar.
    /// </summary>
    public class ActiveEffectSlotView : MonoBehaviour
    {
        [Header("Core widgets")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Duration")]
        [Tooltip("Filled Image (Image Type = Filled, Fill Method = Horizontal).")]
        [SerializeField] private Image durationBar;
        [SerializeField] private TextMeshProUGUI durationLabel;

        [Header("Stack badge")]
        [Tooltip("Badge root — hidden when StackCount == 1.")]
        [SerializeField] private GameObject stackBadgeRoot;
        [SerializeField] private TextMeshProUGUI stackCountText;

        // ── Runtime ──────────────────────────────────────────────────────────────

        private ActiveGameplayEffect _boundEffect;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Bind this slot to an active effect and do the first populate.</summary>
        public void Bind(ActiveGameplayEffect effect)
        {
            _boundEffect = effect;
            gameObject.SetActive(true);

            // Icon
            if (iconImage != null)
            {
                var icon = effect.Effect.icon;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            // Name
            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(effect.Effect.effectName)
                    ? effect.Effect.name
                    : effect.Effect.effectName;

            // Initial duration + stack refresh
            Tick();
        }

        /// <summary>Update the duration bar and labels. Call every frame while the unit is selected.</summary>
        public void Tick()
        {
            if (_boundEffect == null) return;

            UpdateDuration();
            UpdateStack();
        }

        /// <summary>Hide the slot without clearing the bound effect reference.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateDuration()
        {
            float remaining = _boundEffect.RemainingTime;
            float total = _boundEffect.Duration;

            // RemainingTime returns -1 for Infinite; Duration returns -1 for Infinite, 0 for Instant.
            bool isInfinite = remaining < 0f;
            bool isInstant = total == 0f;

            // Duration bar fill
            if (durationBar != null)
            {
                if (isInfinite || isInstant)
                    durationBar.fillAmount = 1f;
                else
                    durationBar.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
            }

            // Duration label
            if (durationLabel != null)
            {
                if (isInfinite)
                    durationLabel.text = "∞";
                else if (isInstant)
                    durationLabel.text = string.Empty;
                else
                    durationLabel.text = remaining.ToString("0.0") + "s";
            }
        }

        private void UpdateStack()
        {
            if (stackBadgeRoot == null) return;

            int stacks = _boundEffect.StackCount;
            bool showBadge = stacks > 1;
            stackBadgeRoot.SetActive(showBadge);

            if (showBadge && stackCountText != null)
                stackCountText.text = "x" + stacks;
        }
    }
}
