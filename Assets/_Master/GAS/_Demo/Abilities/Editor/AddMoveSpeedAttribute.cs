using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace FD.Ability.Editor
{
    /// <summary>
    /// Helper tool to add MoveSpeed attribute to FDAttributeSet
    /// Run once to patch the attribute set
    /// </summary>
    public class AddMoveSpeedAttribute
    {
        [MenuItem("Tools/GAS/Add MoveSpeed Attribute")]
        public static void PatchAttributeSet()
        {
            string filePath = "Assets/_Master/Scripts/Base/FDAttributeSet.cs";
            
            if (!File.Exists(filePath))
            {
                Debug.LogError("FDAttributeSet.cs not found at: " + filePath);
                return;
            }

            string content = File.ReadAllText(filePath);
            bool modified = false;

            // 1. Add property if not exists
            if (!content.Contains("public GameplayAttribute MoveSpeed"))
            {
                content = Regex.Replace(content,
                    @"(public GameplayAttribute ManaRegen \{ get; private set; \})",
                    "$1\n        public GameplayAttribute MoveSpeed { get; private set; }");
                modified = true;
                Debug.Log("✓ Added MoveSpeed property");
            }

            // 2. Add initialization if not exists
            if (!content.Contains("MoveSpeed = new GameplayAttribute()"))
            {
                content = Regex.Replace(content,
                    @"(ManaRegen = new GameplayAttribute\(\);)",
                    "$1\n            MoveSpeed = new GameplayAttribute();");
                modified = true;
                Debug.Log("✓ Added MoveSpeed initialization");
            }

            // 3. Add registration if not exists
            if (!content.Contains("RegisterAttribute(EGameplayAttributeType.MoveSpeed"))
            {
                content = Regex.Replace(content,
                    @"(RegisterAttribute\(EGameplayAttributeType\.ManaRegen, ManaRegen\);)",
                    "$1\n            RegisterAttribute(EGameplayAttributeType.MoveSpeed, MoveSpeed);");
                modified = true;
                Debug.Log("✓ Added MoveSpeed registration");
            }

            // 4. Add default value if not exists
            if (!content.Contains("MoveSpeed.SetBaseValue"))
            {
                content = Regex.Replace(content,
                    @"(// Set default values)",
                    "$1\n            MoveSpeed.SetBaseValue(5f); // Default move speed");
                modified = true;
                Debug.Log("✓ Added MoveSpeed default value");
            }

            // 5. Add subscription if not exists
            if (!content.Contains("MoveSpeed.OnValueChanged += OnMoveSpeedChanged"))
            {
                content = Regex.Replace(content,
                    @"(Mana\.OnValueChanged \+= OnManaChanged;)",
                    "$1\n            MoveSpeed.OnValueChanged += OnMoveSpeedChanged;");
                modified = true;
                Debug.Log("✓ Added MoveSpeed subscription");
            }

            // 6. Add callback method if not exists
            if (!content.Contains("private void OnMoveSpeedChanged"))
            {
                string callback = @"
        private void OnMoveSpeedChanged(float oldValue, float newValue)
        {
            Debug.Log($""MoveSpeed changed: {oldValue} -> {newValue}"");
        }
        ";
                
                content = Regex.Replace(content,
                    @"(private void OnArmorChanged\(float oldValue, float newValue\))",
                    callback + "\n        $1");
                modified = true;
                Debug.Log("✓ Added OnMoveSpeedChanged callback");
            }

            if (modified)
            {
                File.WriteAllText(filePath, content);
                AssetDatabase.Refresh();
                Debug.Log("✅ FDAttributeSet.cs has been patched with MoveSpeed attribute!");
                Debug.Log("Please check the file for any formatting issues.");
            }
            else
            {
                Debug.Log("✓ MoveSpeed attribute already exists in FDAttributeSet");
            }
        }
    }
}
