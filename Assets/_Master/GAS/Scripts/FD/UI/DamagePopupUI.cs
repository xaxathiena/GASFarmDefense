
using DG.Tweening;
using FD.Core;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class DamagePopupUI : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    [SerializeField] private Ease fadeEase = Ease.InQuad;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Sequence activeSequence;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>();
        }
    }

    private void OnDisable()
    {
        activeSequence?.Kill();
        activeSequence = null;
    }

    public void Play(int damageValue, Vector2 anchoredPosition, float deltaMove, float lifetime)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        rectTransform.anchoredPosition = anchoredPosition;
        canvasGroup.alpha = 1f;

        if (damageText != null)
        {
            damageText.text = damageValue.ToString();
        }

        var targetPosition = anchoredPosition + new Vector2(0f, deltaMove);

        activeSequence?.Kill();
        activeSequence = DOTween.Sequence();
        activeSequence.Append(rectTransform.DOAnchorPos(targetPosition, lifetime).SetEase(moveEase));
        activeSequence.Join(canvasGroup.DOFade(0f, lifetime).SetEase(fadeEase));
        activeSequence.OnComplete(() => PoolManager.Destroy(gameObject));
    }

    private IEnumerator AnimateFallback(Vector2 startPosition, Vector2 targetPosition, float lifetime)
    {
        var duration = Mathf.Max(0.01f, lifetime);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            var t = Mathf.Clamp01(elapsed / duration);
            var moveT = t;
            var fadeT = 1f - t;

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, moveT);
            canvasGroup.alpha = fadeT;

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        canvasGroup.alpha = 0f;
        PoolManager.Destroy(gameObject);
    }
}
