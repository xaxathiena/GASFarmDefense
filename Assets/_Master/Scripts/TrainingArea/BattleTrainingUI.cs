using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GAS;

namespace FD.TrainingArea
{
    /// <summary>
    /// Battle Training UI similar to Dota 2 battle training
    /// </summary>
    public class BattleTrainingUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrainingPlayer trainingPlayer;
        [SerializeField] private Transform spawnPoint;
        
        [Header("Spawn Settings")]
        [SerializeField] private GameObject dummyEnemyPrefab;
        [SerializeField] private GameObject allyPrefab;
        [SerializeField] private float spawnRadius = 3f;
        
        [Header("UI Elements")]
        [SerializeField] private Button activateAbilityButton;
        [SerializeField] private Button createEnemyButton;
        [SerializeField] private Button createAllyButton;
        [SerializeField] private Button clearAllButton;
        [SerializeField] private Button resetPlayerButton;
        [SerializeField] private Button findTargetButton;
        
        [Header("Ability Selection UI")]
        [SerializeField] private TMP_Dropdown abilityDropdown;
        [SerializeField] private TMP_InputField abilitySearchField;
        [SerializeField] private TextMeshProUGUI abilityInfoText;
        
        [Header("Enemy Settings UI")]
        [SerializeField] private Toggle enemyInvulnerableToggle;
        [SerializeField] private Toggle enemyAutoRegenToggle;
        [SerializeField] private Slider enemyRegenRateSlider;
        [SerializeField] private TextMeshProUGUI regenRateText;
        
        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI playerHealthText;
        [SerializeField] private TextMeshProUGUI playerManaText;
        [SerializeField] private Slider playerHealthSlider;
        [SerializeField] private Slider playerManaSlider;

        private List<GameObject> spawnedEntities = new List<GameObject>();
        private DummyEnemy currentDummy;
        private List<GameplayAbility> allAbilities = new List<GameplayAbility>();
        private List<GameplayAbility> filteredAbilities = new List<GameplayAbility>();
        private GameplayAbility selectedAbility;

        private void Start()
        {
            LoadAllAbilities();
            SetupUI();
            UpdateAbilityDropdown();
        }

        private void Update()
        {
            UpdatePlayerStats();
        }

        private void SetupUI()
        {
            // Setup button listeners
            if (activateAbilityButton != null)
                activateAbilityButton.onClick.AddListener(OnActivateAbility);
            
            if (createEnemyButton != null)
                createEnemyButton.onClick.AddListener(OnCreateEnemy);
            
            if (createAllyButton != null)
                createAllyButton.onClick.AddListener(OnCreateAlly);
            
            if (clearAllButton != null)
                clearAllButton.onClick.AddListener(OnClearAll);
            
            if (resetPlayerButton != null)
                resetPlayerButton.onClick.AddListener(OnResetPlayer);
            
            if (findTargetButton != null)
                findTargetButton.onClick.AddListener(OnFindTarget);

            // Setup toggle listeners
            if (enemyInvulnerableToggle != null)
                enemyInvulnerableToggle.onValueChanged.AddListener(OnInvulnerableToggle);
            
            if (enemyAutoRegenToggle != null)
                enemyAutoRegenToggle.onValueChanged.AddListener(OnAutoRegenToggle);
            
            // Setup slider listener
            if (enemyRegenRateSlider != null)
            {
                enemyRegenRateSlider.onValueChanged.AddListener(OnRegenRateChanged);
                OnRegenRateChanged(enemyRegenRateSlider.value);
            }

            // Setup dropdown listener
            if (abilityDropdown != null)
                abilityDropdown.onValueChanged.AddListener(OnAbilitySelected);
            
            // Setup search field listener
            if (abilitySearchField != null)
                abilitySearchField.onValueChanged.AddListener(OnSearchTextChanged);
        }

        private void LoadAllAbilities()
        {
            allAbilities.Clear();
#if UNITY_EDITOR
            // Load all ScriptableObject assets and filter by GameplayAbility type
            // This will include all classes that inherit from GameplayAbility
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                // Check if this ScriptableObject is a GameplayAbility or inherits from it
                if (asset != null && asset is GameplayAbility ability)
                {
                    allAbilities.Add(ability);
                }
            }
            
            // Sort by ability name, then by asset name
            allAbilities = allAbilities.OrderBy(a => 
            {
                string name = !string.IsNullOrEmpty(a.abilityName) ? a.abilityName : a.name;
                return name;
            }).ToList();
            
            Debug.Log($"✅ Loaded {allAbilities.Count} abilities from project (including all inherited types)");
            
            // Log ability types for debugging
            var typeGroups = allAbilities.GroupBy(a => a.GetType().Name);
            foreach (var group in typeGroups)
            {
                Debug.Log($"  - {group.Key}: {group.Count()} abilities");
            }
#endif
            filteredAbilities = new List<GameplayAbility>(allAbilities);
        }

        private void UpdateAbilityDropdown()
        {
            if (abilityDropdown == null)
                return;

            abilityDropdown.ClearOptions();
            
            List<string> abilityNames = new List<string> { "--- Select Ability ---" };
            foreach (var ability in filteredAbilities)
            {
                string displayName = !string.IsNullOrEmpty(ability.abilityName) 
                    ? ability.abilityName 
                    : ability.name;
                abilityNames.Add(displayName);
            }
            
            abilityDropdown.AddOptions(abilityNames);
            
            // Show count
            if (abilityInfoText != null && filteredAbilities.Count > 0)
            {
                abilityInfoText.text = $"{filteredAbilities.Count} abilities available";
            }
        }
        
        private void OnSearchTextChanged(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                filteredAbilities = new List<GameplayAbility>(allAbilities);
            }
            else
            {
                string lowerSearch = searchText.ToLower();
                filteredAbilities = allAbilities.Where(a => 
                {
                    string name = !string.IsNullOrEmpty(a.abilityName) ? a.abilityName : a.name;
                    return name.ToLower().Contains(lowerSearch) || 
                           a.GetType().Name.ToLower().Contains(lowerSearch);
                }).ToList();
            }
            
            UpdateAbilityDropdown();
            
            // Reset selection
            if (abilityDropdown != null)
            {
                abilityDropdown.value = 0;
            }
        }

        private void OnActivateAbility()
        {
            if (selectedAbility != null && trainingPlayer != null)
            {
                // Add ability to player if not already added
                trainingPlayer.AddAbility(selectedAbility);
                
                // Get the ability spec to verify it was added
                var abilitySpec = trainingPlayer.AbilitySystemComponent.GetAbilitySpec(selectedAbility);
                    
                if (abilitySpec != null)
                {
                    // Ability was successfully added, now activate it
                    trainingPlayer.ActivateSelectedAbility();
                }
                else
                {
                    Debug.LogWarning($"Failed to add ability: {selectedAbility.name}");
                }
            }
            else if (selectedAbility == null)
            {
                Debug.LogWarning("No ability selected from dropdown!");
            }
        }

        private void OnCreateEnemy()
        {
            if (dummyEnemyPrefab == null)
            {
                Debug.LogWarning("Dummy Enemy Prefab not assigned!");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            GameObject enemy = Instantiate(dummyEnemyPrefab, spawnPosition, Quaternion.identity);
            enemy.name = $"DummyEnemy_{spawnedEntities.Count + 1}";
            spawnedEntities.Add(enemy);
            
            currentDummy = enemy.GetComponent<DummyEnemy>();
            
            // Set target to the new enemy
            if (trainingPlayer != null)
            {
                trainingPlayer.SetTarget(enemy.transform);
            }

            Debug.Log($"Created enemy at {spawnPosition}");
        }

        private void OnCreateAlly()
        {
            if (allyPrefab == null)
            {
                Debug.LogWarning("Ally Prefab not assigned!");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            GameObject ally = Instantiate(allyPrefab, spawnPosition, Quaternion.identity);
            ally.name = $"Ally_{spawnedEntities.Count + 1}";
            spawnedEntities.Add(ally);

            Debug.Log($"Created ally at {spawnPosition}");
        }

        private void OnClearAll()
        {
            foreach (var entity in spawnedEntities)
            {
                if (entity != null)
                {
                    Destroy(entity);
                }
            }
            spawnedEntities.Clear();
            currentDummy = null;
            Debug.Log("Cleared all spawned entities");
        }

        private void OnResetPlayer()
        {
            if (trainingPlayer != null)
            {
                trainingPlayer.ResetStats();
                Debug.Log("Player stats reset");
            }
        }

        private void OnFindTarget()
        {
            if (trainingPlayer != null)
            {
                trainingPlayer.FindNearestTarget();
            }
        }

        private void OnAbilitySelected(int index)
        {
            if (index <= 0 || index > filteredAbilities.Count)
            {
                selectedAbility = null;
                if (abilityInfoText != null)
                {
                    abilityInfoText.text = "No ability selected";
                }
                return;
            }
            
            selectedAbility = filteredAbilities[index - 1];
            UpdateAbilityInfo();
        }

        private void OnInvulnerableToggle(bool value)
        {
            if (currentDummy != null)
            {
                currentDummy.SetInvulnerable(value);
            }
        }

        private void OnAutoRegenToggle(bool value)
        {
            if (currentDummy != null)
            {
                currentDummy.SetAutoRegen(value);
            }
        }

        private void OnRegenRateChanged(float value)
        {
            if (regenRateText != null)
            {
                regenRateText.text = $"{value:F1} HP/s";
            }
            
            if (currentDummy != null)
            {
                currentDummy.SetRegenRate(value);
            }
        }

        private void UpdateAbilityInfo()
        {
            if (abilityInfoText == null)
                return;

            if (selectedAbility != null)
            {
                string displayName = !string.IsNullOrEmpty(selectedAbility.abilityName) 
                    ? selectedAbility.abilityName 
                    : selectedAbility.name;
                    
                string info = $"<b>{displayName}</b>\n";
                info += $"Type: {selectedAbility.GetType().Name}\n";
                
                if (!string.IsNullOrEmpty(selectedAbility.description))
                {
                    info += $"\n{selectedAbility.description}";
                }
                
                abilityInfoText.text = info;
            }
            else
            {
                abilityInfoText.text = "No ability selected";
            }
        }

        private void UpdatePlayerStats()
        {
            if (trainingPlayer == null || trainingPlayer.AttributeSet == null)
                return;

            var attributeSet = trainingPlayer.AttributeSet;

            // Update health
            float health = attributeSet.Health.CurrentValue;
            float maxHealth = attributeSet.MaxHealth.CurrentValue;
            
            if (playerHealthText != null)
                playerHealthText.text = $"HP: {health:F0}/{maxHealth:F0}";
            
            if (playerHealthSlider != null)
            {
                playerHealthSlider.maxValue = maxHealth;
                playerHealthSlider.value = health;
            }

            // Update mana
            float mana = attributeSet.Mana.CurrentValue;
            float maxMana = attributeSet.MaxMana.CurrentValue;
            
            if (playerManaText != null)
                playerManaText.text = $"MP: {mana:F0}/{maxMana:F0}";
            
            if (playerManaSlider != null)
            {
                playerManaSlider.maxValue = maxMana;
                playerManaSlider.value = mana;
            }
        }

        private Vector3 GetSpawnPosition()
        {
            if (spawnPoint != null)
            {
                // Random position around spawn point
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                return spawnPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            
            // Fallback to random position around player
            Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
            return trainingPlayer.transform.position + new Vector3(randomPos.x, 0, randomPos.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
            }
        }
    }
}
