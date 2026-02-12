using UnityEngine;
using UnityEditor;
using FD.UI;
using TMPro;
using UnityEngine.UI;

namespace FD.Editor
{
    /// <summary>
    /// Editor utility to create Status Effect Display components
    /// Menu: FD/Create/Status Effect Setup
    /// </summary>
    public static class StatusEffectSetupUtility
    {
        [MenuItem("FD/Create/Status Effect Icon Prefab")]
        public static void CreateStatusEffectIconPrefab()
        {
            // Create root object
            GameObject iconGO = new GameObject("StatusEffectIcon");
            RectTransform iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(40, 40);
            
            CanvasGroup canvasGroup = iconGO.AddComponent<CanvasGroup>();
            StatusEffectIcon iconComponent = iconGO.AddComponent<StatusEffectIcon>();

            // Create IconBackground
            GameObject bgGO = new GameObject("IconBackground");
            bgGO.transform.SetParent(iconGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Create FillImage
            GameObject fillGO = new GameObject("FillImage");
            fillGO.transform.SetParent(iconGO.transform, false);
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillAmount = 1f;
            fillImage.color = new Color(0.3f, 0.6f, 1f, 0.6f);

            // Create StackText
            GameObject stackGO = new GameObject("StackText");
            stackGO.transform.SetParent(iconGO.transform, false);
            RectTransform stackRect = stackGO.AddComponent<RectTransform>();
            stackRect.anchorMin = new Vector2(1, 1);
            stackRect.anchorMax = new Vector2(1, 1);
            stackRect.pivot = new Vector2(1, 1);
            stackRect.anchoredPosition = new Vector2(-2, -2);
            stackRect.sizeDelta = new Vector2(20, 20);
            TextMeshProUGUI stackText = stackGO.AddComponent<TextMeshProUGUI>();
            stackText.fontSize = 12;
            stackText.alignment = TextAlignmentOptions.BottomRight;
            stackText.color = Color.white;
            stackText.text = "1";
            stackText.fontStyle = FontStyles.Bold;

            // Create TimerText
            GameObject timerGO = new GameObject("TimerText");
            timerGO.transform.SetParent(iconGO.transform, false);
            RectTransform timerRect = timerGO.AddComponent<RectTransform>();
            timerRect.anchorMin = Vector2.zero;
            timerRect.anchorMax = Vector2.one;
            timerRect.offsetMin = Vector2.zero;
            timerRect.offsetMax = Vector2.zero;
            TextMeshProUGUI timerText = timerGO.AddComponent<TextMeshProUGUI>();
            timerText.fontSize = 10;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = Color.white;
            timerText.text = "5.0";
            timerText.fontStyle = FontStyles.Bold;

            // Assign references
            SerializedObject serializedIcon = new SerializedObject(iconComponent);
            serializedIcon.FindProperty("iconImage").objectReferenceValue = bgImage;
            serializedIcon.FindProperty("fillImage").objectReferenceValue = fillImage;
            serializedIcon.FindProperty("stackText").objectReferenceValue = stackText;
            serializedIcon.FindProperty("timerText").objectReferenceValue = timerText;
            serializedIcon.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedIcon.ApplyModifiedProperties();

            // Save as prefab
            string path = "Assets/Prefabs/UI/StatusEffectIcon.prefab";
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(iconGO, path);
            
            // Clean up scene object
            Object.DestroyImmediate(iconGO);

            // Select the prefab
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"Created StatusEffectIcon prefab at: {path}");
        }

        [MenuItem("FD/Create/Status Effect Icon Database")]
        public static void CreateStatusEffectIconDatabase()
        {
            StatusEffectIconDatabase database = ScriptableObject.CreateInstance<StatusEffectIconDatabase>();

            string path = "Assets/Resources/UI/StatusEffectIconDatabase.asset";
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(database, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the asset
            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);

            Debug.Log($"Created StatusEffectIconDatabase at: {path}");
            Debug.Log("Right-click the asset and select 'Add All Tags' to populate with all GameplayTags");
        }

        [MenuItem("FD/Create/Status Effect Canvas")]
        public static void CreateStatusEffectCanvas()
        {
            // Create Canvas
            GameObject canvasGO = new GameObject("StatusEffectCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create container
            GameObject containerGO = new GameObject("StatusEffectContainer");
            containerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(400, 50);

            HorizontalLayoutGroup layout = containerGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Select the canvas
            Selection.activeGameObject = canvasGO;

            Debug.Log("Created StatusEffectCanvas with container. Assign this to StatusEffectDisplayManager.");
        }

        [MenuItem("GameObject/FD/Add Status Effect Display", false, 10)]
        public static void AddStatusEffectDisplayToSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("Please select a GameObject first.");
                return;
            }

            // Check if it has AbilitySystemComponent
            if (selected.GetComponent<GAS.AbilitySystemComponent>() == null)
            {
                Debug.LogWarning($"{selected.name} does not have an AbilitySystemComponent. Adding it...");
                //selected.AddComponent<GAS.AbilitySystemComponent>();
            }

            // Add StatusEffectDisplayHandler
            if (selected.GetComponent<FD.Character.StatusEffectDisplayHandler>() == null)
            {
                var handler = selected.AddComponent<FD.Character.StatusEffectDisplayHandler>();
                Debug.Log($"Added StatusEffectDisplayHandler to {selected.name}");
            }
            else
            {
                Debug.LogWarning($"{selected.name} already has a StatusEffectDisplayHandler.");
            }
        }
    }
}
