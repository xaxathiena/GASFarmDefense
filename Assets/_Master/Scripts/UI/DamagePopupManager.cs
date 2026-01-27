using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    [SerializeField] private DamagePopupUI popupPrefab;
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float popupRiseDistance = 60f;
    [SerializeField] private float popupLifetime = 1f;

    private Canvas cachedCanvas;

    private void Awake()
    {
        cachedCanvas = popupRoot != null ? popupRoot.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
    }

    public void ShowDamage(Transform target, float damageAmount)
    {
        if (popupPrefab == null || popupRoot == null || target == null)
        {
            return;
        }

        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        var screenPos = cam.WorldToScreenPoint(target.position);
        if (screenPos.z < 0f)
        {
            return;
        }

        var popupInstance = Instantiate(popupPrefab, popupRoot);
        var canvas = cachedCanvas != null ? cachedCanvas : popupRoot.GetComponentInParent<Canvas>();
        var canvasCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRoot, screenPos, canvasCamera, out var anchoredPosition))
        {
            popupInstance.Play(Mathf.RoundToInt(damageAmount), anchoredPosition, popupRiseDistance, popupLifetime);
        }
        else
        {
            Destroy(popupInstance.gameObject);
        }
    }
}
