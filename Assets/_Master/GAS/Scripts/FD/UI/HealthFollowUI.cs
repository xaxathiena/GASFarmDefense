using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class HealthFollowUI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset = Vector3.up;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private bool hideWhenOffScreen = true;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private RectTransform canvasRectTransform;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = rectTransform.GetComponentInParent<Canvas>();
        canvasRectTransform = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TMP_Text>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void LateUpdate()
    {
        if (target == null || rectTransform == null || canvasRectTransform == null)
        {
            return;
        }

        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        var worldPosition = target.position + worldOffset;
        var screenPos = cam.WorldToScreenPoint(worldPosition);
        var canvasCam = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;

        if (screenPos.z <= 0f)
        {
            UpdateVisibility(false);
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPos, canvasCam, out var localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
            UpdateVisibility(true);
        }
        else
        {
            UpdateVisibility(false);
        }
    }

    private void UpdateVisibility(bool isVisible)
    {
        if (!hideWhenOffScreen)
        {
            isVisible = true;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.blocksRaycasts = isVisible;
            canvasGroup.interactable = isVisible;
        }
        else if (healthText != null)
        {
            healthText.enabled = isVisible;
        }
    }

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
    }

    public void SetHealth(int currentHealth)
    {
        if (healthText != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }
}
