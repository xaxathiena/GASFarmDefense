using UnityEngine;
using UnityEngine.UI;

namespace FD.TrainingArea
{
    /// <summary>
    /// Helper component to automatically layout UI buttons in a grid
    /// Add this to a panel to auto-arrange its children
    /// </summary>
    [ExecuteInEditMode]
    public class UILayoutHelper : MonoBehaviour
    {
        [Header("Layout Settings")]
        [SerializeField] private bool autoLayout = true;
        [SerializeField] private float spacing = 10f;
        [SerializeField] private float buttonWidth = 120f;
        [SerializeField] private float buttonHeight = 40f;
        [SerializeField] private int columns = 3;
        [SerializeField] private Vector2 startPosition = new Vector2(-250, 200);

        private void Update()
        {
            if (autoLayout && Application.isEditor)
            {
                LayoutChildren();
            }
        }

        [ContextMenu("Layout Children")]
        public void LayoutChildren()
        {
            int index = 0;
            foreach (RectTransform child in transform)
            {
                if (child == null) continue;

                int row = index / columns;
                int col = index % columns;

                float x = startPosition.x + col * (buttonWidth + spacing);
                float y = startPosition.y - row * (buttonHeight + spacing);

                child.anchoredPosition = new Vector2(x, y);
                child.sizeDelta = new Vector2(buttonWidth, buttonHeight);

                index++;
            }
        }

        [ContextMenu("Add Text to Buttons")]
        public void AddTextToButtons()
        {
            foreach (Transform child in transform)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    // Check if it already has a text child
                    var textChild = child.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (textChild == null)
                    {
                        // Create text GameObject
                        GameObject textObj = new GameObject("Text");
                        textObj.transform.SetParent(child, false);

                        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = Vector2.zero;
                        rectTransform.anchoredPosition = Vector2.zero;

                        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                        text.text = child.name.Replace("Btn", "").Replace("Button", "");
                        text.alignment = TMPro.TextAlignmentOptions.Center;
                        text.color = Color.black;
                        text.fontSize = 14;
                    }
                }
            }
        }

        [ContextMenu("Setup Panel Background")]
        public void SetupPanelBackground()
        {
            var image = GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Position at bottom
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.pivot = new Vector2(0.5f, 0);
                rectTransform.anchoredPosition = new Vector2(0, 0);
                rectTransform.sizeDelta = new Vector2(0, 250);
            }
        }
    }
}
