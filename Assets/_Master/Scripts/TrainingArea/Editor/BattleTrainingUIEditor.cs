using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace FD.TrainingArea.Editor
{
    [CustomEditor(typeof(BattleTrainingUI))]
    public class BattleTrainingUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setup Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Auto Setup UI", GUILayout.Height(30)))
            {
                AutoSetupUI();
            }

            if (GUILayout.Button("Layout Buttons", GUILayout.Height(25)))
            {
                LayoutButtons();
            }

            if (GUILayout.Button("Add Button Texts", GUILayout.Height(25)))
            {
                AddButtonTexts();
            }

            if (GUILayout.Button("Wire Up References", GUILayout.Height(25)))
            {
                WireUpReferences();
            }

            if (GUILayout.Button("Create Ability Search UI", GUILayout.Height(25)))
            {
                CreateAbilitySearchUI();
            }
        }

        private void AutoSetupUI()
        {
            AddButtonTexts();
            LayoutButtons();
            LayoutPanels();
            WireUpReferences();
            EditorUtility.SetDirty(target);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("✅ Battle Training UI setup complete!");
        }

        private void AddButtonTexts()
        {
            var ui = (BattleTrainingUI)target;
            var buttons = ui.GetComponentsInChildren<Button>(true);

            foreach (var button in buttons)
            {
                var existingText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (existingText == null)
                {
                    var textObj = new GameObject("Text");
                    textObj.transform.SetParent(button.transform, false);

                    var rectTransform = textObj.AddComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;

                    existingText = textObj.AddComponent<TextMeshProUGUI>();
                }

                existingText.text = GetButtonLabel(button.name);
                existingText.alignment = TextAlignmentOptions.Center;
                existingText.color = Color.white;
                existingText.fontSize = 14;
                existingText.fontStyle = FontStyles.Bold;
            }

            Debug.Log($"✅ Added text to {buttons.Length} buttons");
        }

        private void LayoutButtons()
        {
            var ui = (BattleTrainingUI)target;
            var mainPanel = ui.transform.Find("MainPanel");

            if (mainPanel != null)
            {
                LayoutButtonsInPanel(mainPanel, new string[]
                {
                    "ActivateAbilityBtn",
                    "CreateEnemyBtn",
                    "CreateAllyBtn",
                    "ClearAllBtn",
                    "ResetPlayerBtn",
                    "FindTargetBtn"
                }, 120, 50, 10);
            }

            Debug.Log("✅ Buttons laid out");
        }

        private void LayoutPanels()
        {
            var ui = (BattleTrainingUI)target;

            // Main Panel (bottom center)
            var mainPanel = ui.transform.Find("MainPanel");
            if (mainPanel != null)
            {
                var rect = mainPanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(0, 10);
                rect.sizeDelta = new Vector2(800, 100);

                var image = mainPanel.GetComponent<Image>();
                if (image != null) image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }

            // Player Stats Panel (top left)
            var statsPanel = ui.transform.Find("PlayerStatsPanel");
            if (statsPanel != null)
            {
                var rect = statsPanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(10, -10);
                rect.sizeDelta = new Vector2(250, 150);

                var image = statsPanel.GetComponent<Image>();
                if (image != null) image.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            }

            // Control Panel (right side)
            var controlPanel = ui.transform.Find("ControlPanel");
            if (controlPanel != null)
            {
                var rect = controlPanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f);
                rect.pivot = new Vector2(1, 0.5f);
                rect.anchoredPosition = new Vector2(-10, 0);
                rect.sizeDelta = new Vector2(200, 300);

                var image = controlPanel.GetComponent<Image>();
                if (image != null) image.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            }

            Debug.Log("✅ Panels positioned");
        }

        private void LayoutButtonsInPanel(Transform panel, string[] buttonNames, float buttonWidth, float buttonHeight, float spacing)
        {
            float totalWidth = buttonNames.Length * buttonWidth + (buttonNames.Length - 1) * spacing;
            float startX = -totalWidth / 2 + buttonWidth / 2;

            for (int i = 0; i < buttonNames.Length; i++)
            {
                var button = panel.Find(buttonNames[i]);
                if (button != null)
                {
                    var rect = button.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(startX + i * (buttonWidth + spacing), 0);
                    rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                }
            }
        }

        private void WireUpReferences()
        {
            var ui = (BattleTrainingUI)target;
            var so = new SerializedObject(ui);

            // Find references
            var trainingPlayer = FindObjectOfType<TrainingPlayer>();
            var spawnPoint = GameObject.Find("SpawnPoint");

            // Set references
            so.FindProperty("trainingPlayer").objectReferenceValue = trainingPlayer;
            so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint?.transform;

            // Wire up buttons
            so.FindProperty("activateAbilityButton").objectReferenceValue = FindButton(ui.transform, "ActivateAbilityBtn");
            so.FindProperty("createEnemyButton").objectReferenceValue = FindButton(ui.transform, "CreateEnemyBtn");
            so.FindProperty("createAllyButton").objectReferenceValue = FindButton(ui.transform, "CreateAllyBtn");
            so.FindProperty("clearAllButton").objectReferenceValue = FindButton(ui.transform, "ClearAllBtn");
            so.FindProperty("resetPlayerButton").objectReferenceValue = FindButton(ui.transform, "ResetPlayerBtn");
            so.FindProperty("findTargetButton").objectReferenceValue = FindButton(ui.transform, "FindTargetBtn");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ui);

            Debug.Log("✅ All references wired up!");
        }

        private void CreateAbilitySearchUI()
        {
            var ui = (BattleTrainingUI)target;
            var controlPanel = ui.transform.Find("ControlPanel");
            
            if (controlPanel == null)
            {
                Debug.LogError("ControlPanel not found!");
                return;
            }

            // Create Ability Selection Container
            var abilityContainer = CreateUIObject("AbilitySelectionContainer", controlPanel);
            var containerRect = abilityContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = new Vector2(0, -10);
            containerRect.sizeDelta = new Vector2(-20, 250);

            // Add Vertical Layout Group
            var layoutGroup = abilityContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            // Create Title
            var title = CreateText("AbilityTitle", abilityContainer.transform, "ABILITY SELECTION");
            var titleLayout = title.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 25;
            var titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;

            // Create Search Field
            var searchObj = CreateInputField("AbilitySearchField", abilityContainer.transform, "Search abilities...");
            var searchLayout = searchObj.AddComponent<LayoutElement>();
            searchLayout.preferredHeight = 30;

            // Create Dropdown
            var dropdownObj = CreateDropdown("AbilityDropdown", abilityContainer.transform);
            var dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
            dropdownLayout.preferredHeight = 35;

            // Create Info Text
            var infoObj = CreateText("AbilityInfoText", abilityContainer.transform, "Select an ability");
            var infoLayout = infoObj.AddComponent<LayoutElement>();
            infoLayout.preferredHeight = 120;
            infoLayout.flexibleHeight = 1;
            var infoText = infoObj.GetComponent<TextMeshProUGUI>();
            infoText.fontSize = 11;
            infoText.alignment = TextAlignmentOptions.TopLeft;

            // Wire up references
            var so = new SerializedObject(ui);
            so.FindProperty("abilitySearchField").objectReferenceValue = searchObj.GetComponent<TMP_InputField>();
            so.FindProperty("abilityDropdown").objectReferenceValue = dropdownObj.GetComponent<TMP_Dropdown>();
            so.FindProperty("abilityInfoText").objectReferenceValue = infoText;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(ui);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            
            Debug.Log("✅ Ability Search UI created in ControlPanel!");
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            return obj;
        }

        private GameObject CreateText(string name, Transform parent, string text)
        {
            var obj = CreateUIObject(name, parent);
            var textComp = obj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.color = Color.white;
            textComp.fontSize = 12;
            return obj;
        }

        private GameObject CreateInputField(string name, Transform parent, string placeholder)
        {
            var obj = CreateUIObject(name, parent);
            var image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var inputField = obj.AddComponent<TMP_InputField>();
            
            // Create Text Area
            var textArea = CreateUIObject("Text Area", obj.transform);
            var textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-10, -10);
            
            // Create Placeholder
            var placeholderObj = CreateText("Placeholder", textArea.transform, placeholder);
            var placeholderText = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.fontStyle = FontStyles.Italic;
            
            // Create Text
            var textObj = CreateText("Text", textArea.transform, "");
            
            inputField.textViewport = textAreaRect;
            inputField.textComponent = textObj.GetComponent<TextMeshProUGUI>();
            inputField.placeholder = placeholderText;
            
            return obj;
        }

        private GameObject CreateDropdown(string name, Transform parent)
        {
            var obj = CreateUIObject(name, parent);
            var image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var dropdown = obj.AddComponent<TMP_Dropdown>();
            
            // Create Label
            var labelObj = CreateText("Label", obj.transform, "Option A");
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = new Vector2(-25, 0);
            labelRect.offsetMin = new Vector2(10, 0);
            
            // Create Arrow
            var arrowObj = CreateUIObject("Arrow", obj.transform);
            var arrowImage = arrowObj.AddComponent<Image>();
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            
            // Create Template
            var template = CreateDropdownTemplate(obj.transform);
            
            dropdown.targetGraphic = image;
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.captionText = labelObj.GetComponent<TextMeshProUGUI>();
            dropdown.itemText = template.Find("Viewport/Content/Item/Item Label").GetComponent<TextMeshProUGUI>();
            
            template.gameObject.SetActive(false);
            
            return obj;
        }

        private Transform CreateDropdownTemplate(Transform parent)
        {
            var template = CreateUIObject("Template", parent);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            
            var templateImage = template.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            var scrollRect = template.AddComponent<ScrollRect>();
            
            // Viewport
            var viewport = CreateUIObject("Viewport", template.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            
            // Content
            var content = CreateUIObject("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);
            
            // Item
            var item = CreateUIObject("Item", content.transform);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 20);
            
            var itemToggle = item.AddComponent<Toggle>();
            var itemBackground = item.AddComponent<Image>();
            itemBackground.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            
            var itemLabel = CreateText("Item Label", item.transform, "Option A");
            var itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.sizeDelta = new Vector2(-10, 0);
            
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            return template.transform;
        }

        private Button FindButton(Transform root, string name)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            return buttons.FirstOrDefault(b => b.name == name);
        }

        private string GetButtonLabel(string buttonName)
        {
            return buttonName
                .Replace("Btn", "")
                .Replace("Button", "")
                .Replace("ActivateAbility", "Activate Ability")
                .Replace("CreateEnemy", "Create Enemy")
                .Replace("CreateAlly", "Create Ally")
                .Replace("ClearAll", "Clear All")
                .Replace("ResetPlayer", "Reset Player")
                .Replace("FindTarget", "Find Target");
        }
    }
}
