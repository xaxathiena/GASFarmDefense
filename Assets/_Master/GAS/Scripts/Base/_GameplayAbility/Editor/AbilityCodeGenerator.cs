using UnityEngine;
using UnityEditor;
using System.IO;

namespace GAS.Editor
{
    /// <summary>
    /// Editor tool to generate new ability Data + Behaviour files.
    /// Usage: Tools/Abel/GAS/Create New Ability
    /// </summary>
    public class AbilityCodeGenerator : EditorWindow
    {
        private string abilityName = "NewAbility";
        private string dataFolder = "Assets/_Master/GAS/Scripts/FD/Abilities";
        private string behaviourFolder = "Assets/_Master/GAS/Scripts/FD/Abilities";

        [MenuItem("Tools/Abel/GAS/Create New Ability")]
        public static void ShowWindow()
        {
            var window = GetWindow<AbilityCodeGenerator>("Create Ability");
            window.minSize = new Vector2(400, 250);
        }

        private void OnGUI()
        {
            GUILayout.Label("Generate New Ability", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            abilityName = EditorGUILayout.TextField("Ability Name:", abilityName);
            EditorGUILayout.HelpBox("Enter ability name in PascalCase (e.g., 'Fireball', 'HealingWave')", MessageType.Info);

            EditorGUILayout.Space();

            dataFolder = EditorGUILayout.TextField("Data Folder:", dataFolder);
            behaviourFolder = EditorGUILayout.TextField("Behaviour Folder:", behaviourFolder);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Files", GUILayout.Height(40)))
            {
                GenerateAbilityFiles();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will create:\n" +
                $"1. {abilityName}Data.cs (ScriptableObject)\n" +
                $"2. {abilityName}Behaviour.cs (Logic class)\n\n" +
                "Remember to register the behaviour in VContainer!",
                MessageType.Info
            );
        }

        private void GenerateAbilityFiles()
        {
            if (string.IsNullOrWhiteSpace(abilityName))
            {
                EditorUtility.DisplayDialog("Error", "Ability name cannot be empty!", "OK");
                return;
            }

            // Ensure folders exist
            Directory.CreateDirectory(dataFolder);
            Directory.CreateDirectory(behaviourFolder);

            // Generate Data file
            string dataPath = Path.Combine(dataFolder, $"{abilityName}Data.cs");
            string dataContent = GenerateDataTemplate(abilityName);
            File.WriteAllText(dataPath, dataContent);

            // Generate Behaviour file
            string behaviourPath = Path.Combine(behaviourFolder, $"{abilityName}Behaviour.cs");
            string behaviourContent = GenerateBehaviourTemplate(abilityName);
            File.WriteAllText(behaviourPath, behaviourContent);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success!",
                $"Created:\n{dataPath}\n{behaviourPath}\n\n" +
                $"Next steps:\n" +
                $"1. Register {abilityName}Behaviour in VContainer\n" +
                $"2. Create {abilityName}Data asset from menu\n" +
                $"3. Implement ability logic in {abilityName}Behaviour",
                "OK"
            );

            Close();
        }

        private string GenerateDataTemplate(string name)
        {
            return $@"using UnityEngine;
using GAS;

namespace FD.Abilities
{{
    /// <summary>
    /// Data configuration for {name} ability - PURE DATA ONLY.
    /// Add custom fields here for ability-specific parameters.
    /// Behaviour type mapping is handled by GameplayAbilityLogic (auto-detected by convention).
    /// </summary>
    [CreateAssetMenu(fileName = ""{name}"", menuName = ""GAS/Abilities/{name}"")]
    public class {name}Data : GameplayAbilityData
    {{
        [Header(""{name} Settings"")]
        // Add your custom fields here
        // Example:
        // public float damage = 50f;
        // public GameObject projectilePrefab;
        // public float projectileSpeed = 20f;
    }}
}}
";
        }

        private string GenerateBehaviourTemplate(string name)
        {
            return $@"using UnityEngine;
using GAS;

namespace FD.Abilities
{{
    /// <summary>
    /// Behaviour logic for {name} ability.
    /// Implement ability-specific logic here.
    /// This class is a Singleton and should be stateless.
    /// </summary>
    public class {name}Behaviour : IAbilityBehaviour
    {{
        // Inject any services you need via constructor
        private readonly IDebugService debug;
        
        public {name}Behaviour(IDebugService debug)
        {{
            this.debug = debug;
        }}

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {{
            // Add custom activation checks here
            // Example: check target in range, check resources, etc.
            
            return true; // Base checks are already handled by GameplayAbilityLogic
        }}

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {{
            var {name.ToLower()}Data = data as {name}Data;
            if ({name.ToLower()}Data == null)
            {{
                Debug.LogError(""Invalid data type for {name}Behaviour"");
                return;
            }}

            // Implement ability logic here
            // Example:
            // - Spawn projectiles
            // - Apply damage/healing
            // - Play VFX/SFX
            // - Trigger animations
            
            debug.Log($""{name} activated!"", Color.cyan);
            
            // Example of ending ability immediately (for instant abilities)
            // asc.EndAbility({name.ToLower()}Data);
        }}

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {{
            // Clean up ability state here
            // Example: destroy projectiles, stop VFX, etc.
            
            debug.Log($""{name} ended"", Color.gray);
        }}

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {{
            // Handle cancellation here
            // Example: refund partial costs, interrupt animations, etc.
            
            debug.Log($""{name} cancelled"", Color.yellow);
        }}
    }}
}}
";
        }
    }
}
