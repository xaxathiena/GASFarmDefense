using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FD.TrainingArea
{
    /// <summary>
    /// Auto-setup script for Battle Training UI. Attach to UI GameObject and run in editor.
    /// </summary>
    [ExecuteInEditMode]
    public class BattleTrainingUISetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool runSetup = false;

        private void Update()
        {
            if (runSetup && !Application.isPlaying)
            {
                runSetup = false;
                SetupUI();
            }
        }

        [ContextMenu("Setup Battle Training UI")]
        public void SetupUI()
        {
            Debug.Log("=== Starting Battle Training UI Setup ===");
            
            // Find references
            var trainingPlayer = FindObjectOfType<TrainingPlayer>();
            var spawnPoint = GameObject.Find("SpawnPoint");
            var battleUI = GetComponent<BattleTrainingUI>();
            
            if (battleUI == null)
            {
                Debug.LogError("BattleTrainingUI component not found!");
                return;
            }

            // Setup panels
            SetupMainPanel();
            SetupControlPanel();
            SetupPlayerStatsPanel();
            
            // Add text to buttons
            AddTextToAllButtons();
            
            // Wire up references
            WireUpReferences(battleUI, trainingPlayer, spawnPoint);
            
            Debug.Log("=== Battle Training UI Setup Complete! ===");
        }

        private void SetupMainPanel()
        {
            var mainPanel = transform.Find("MainPanel");
            if (mainPanel == null)
            {
                Debug.LogWarning("MainPanel not found");
                return;
            }

            var rect = mainPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Position at bottom center
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(0, 10);
                rect.sizeDelta = new Vector2(800, 120);
            }

            var image = mainPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }

            // Layout buttons
            LayoutButtonsInPanel(mainPanel, new string[]
            {
                "ActivateAbilityBtn",
                "CreateEnemyBtn",
                "CreateAllyBtn",
                "ClearAllBtn",
                "ResetPlayerBtn",
                "FindTargetBtn"
            }, 130, 50, 10);
        }

        private void SetupControlPanel()
        {
            var controlPanel = transform.Find("ControlPanel");
            if (controlPanel == null)
            {
                Debug.LogWarning("ControlPanel not found");
                return;
            }

            var rect = controlPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Position at right side
                rect.anchorMin = new Vector2(1, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f);
                rect.pivot = new Vector2(1, 0.5f);
                rect.anchoredPosition = new Vector2(-10, 0);
                rect.sizeDelta = new Vector2(200, 300);
            }

            var image = controlPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            }
        }

        private void SetupPlayerStatsPanel()
        {
            var statsPanel = transform.Find("PlayerStatsPanel");
            if (statsPanel == null)
            {
                Debug.LogWarning("PlayerStatsPanel not found");
                return;
            }

            var rect = statsPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Position at top left
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(10, -10);
                rect.sizeDelta = new Vector2(250, 150);
            }

            var image = statsPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            }
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
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = new Vector2(startX + i * (buttonWidth + spacing), 0);
                        rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                    }
                }
            }
        }

        private void AddTextToAllButtons()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                AddTextToButton(button.transform, button.name);
            }
        }

        private void AddTextToButton(Transform buttonTransform, string buttonName)
        {
            // Check if text already exists
            var existingText = buttonTransform.GetComponentInChildren<TextMeshProUGUI>();
            if (existingText != null)
            {
                // Update existing text
                existingText.text = GetButtonLabel(buttonName);
                return;
            }

            // Create new text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonTransform, false);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = GetButtonLabel(buttonName);
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;

            Debug.Log($"Added text to {buttonName}: {text.text}");
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

        private void WireUpReferences(BattleTrainingUI battleUI, TrainingPlayer player, GameObject spawnPoint)
        {
            Debug.Log("Wiring up references...");

            // Use reflection to set private fields
            var type = typeof(BattleTrainingUI);
            
            // Set references
            SetField(type, battleUI, "trainingPlayer", player);
            SetField(type, battleUI, "spawnPoint", spawnPoint?.transform);

            // Find and set UI elements
            SetField(type, battleUI, "activateAbilityButton", FindButton("ActivateAbilityBtn"));
            SetField(type, battleUI, "createEnemyButton", FindButton("CreateEnemyBtn"));
            SetField(type, battleUI, "createAllyButton", FindButton("CreateAllyBtn"));
            SetField(type, battleUI, "clearAllButton", FindButton("ClearAllBtn"));
            SetField(type, battleUI, "resetPlayerButton", FindButton("ResetPlayerBtn"));
            SetField(type, battleUI, "findTargetButton", FindButton("FindTargetBtn"));

            Debug.Log("All references wired up!");
        }

        private Button FindButton(string name)
        {
            var obj = GameObject.Find(name);
            return obj?.GetComponent<Button>();
        }

        private void SetField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
                Debug.Log($"Set {fieldName} = {value}");
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found");
            }
        }
    }
}
