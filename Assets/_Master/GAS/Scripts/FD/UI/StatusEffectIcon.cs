using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GAS;

namespace FD.UI
{
    /// <summary>
    /// UI component for displaying a single status effect icon with timer
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class StatusEffectIcon : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text stackText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.15f;
        [SerializeField] private float pulseScale = 1.1f;
        [SerializeField] private float pulseDuration = 0.3f;

        private ActiveGameplayEffect activeEffect;
        private RectTransform rectTransform;
        private Sequence activeSequence;
        private bool isInitialized;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
        }

        private void OnDisable()
        {
            activeSequence?.Kill();
            activeSequence = null;
        }

        /// <summary>
        /// Initialize and show the status effect icon
        /// </summary>
        public void Initialize(ActiveGameplayEffect effect, Sprite icon)
        {
            activeEffect = effect;

            // Set icon
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
            }

            // Initial update
            UpdateDisplay();

            // Fade in animation
            canvasGroup.alpha = 0f;
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();
            activeSequence.Append(canvasGroup.DOFade(1f, fadeInDuration));
            activeSequence.Join(rectTransform.DOScale(Vector3.one * pulseScale, fadeInDuration * 0.5f));
            activeSequence.Append(rectTransform.DOScale(Vector3.one, fadeInDuration * 0.5f));

            isInitialized = true;
        }

        /// <summary>
        /// Update the effect display (call each frame)
        /// </summary>
        public void UpdateDisplay()
        {
            if (activeEffect == null || activeEffect.Effect == null)
                return;

            // Update stack count
            if (stackText != null)
            {
                if (activeEffect.StackCount > 1)
                {
                    stackText.gameObject.SetActive(true);
                    stackText.text = activeEffect.StackCount.ToString();
                }
                else
                {
                    stackText.gameObject.SetActive(false);
                }
            }

            // Update timer and fill
            if (activeEffect.Duration > 0)
            {
                float remaining = activeEffect.RemainingTime;
                float percentage = remaining / activeEffect.Duration;

                // Update fill image
                if (fillImage != null)
                {
                    fillImage.fillAmount = percentage;
                }

                // Update timer text
                if (timerText != null)
                {
                    if (remaining > 99f)
                    {
                        timerText.text = Mathf.CeilToInt(remaining).ToString();
                    }
                    else if (remaining > 10f)
                    {
                        timerText.text = remaining.ToString("F0");
                    }
                    else
                    {
                        timerText.text = remaining.ToString("F1");
                    }
                }
            }
            else if (activeEffect.Duration < 0) // Infinite
            {
                if (fillImage != null)
                    fillImage.fillAmount = 1f;

                if (timerText != null)
                    timerText.text = "∞";
            }
            else // Instant
            {
                if (fillImage != null)
                    fillImage.fillAmount = 0f;

                if (timerText != null)
                    timerText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Play stack added animation
        /// </summary>
        public void PlayStackAnimation()
        {
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();
            activeSequence.Append(rectTransform.DOScale(Vector3.one * pulseScale, pulseDuration * 0.5f));
            activeSequence.Append(rectTransform.DOScale(Vector3.one, pulseDuration * 0.5f));
        }

        /// <summary>
        /// Fade out and destroy
        /// </summary>
        public void FadeOut(System.Action onComplete = null)
        {
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();
            activeSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration));
            activeSequence.Join(rectTransform.DOScale(Vector3.one * 0.5f, fadeOutDuration));
            activeSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            });
        }

        public ActiveGameplayEffect GetActiveEffect() => activeEffect;
        public bool IsInitialized() => isInitialized;
    }
}
