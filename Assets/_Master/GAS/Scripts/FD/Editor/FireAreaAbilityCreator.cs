using UnityEngine;
using UnityEditor;
using FD.Ability;
using GAS;

namespace FD.Editor
{
    /// <summary>
    /// Editor utility to create FireAreaAbility asset
    /// </summary>
    public static class FireAreaAbilityCreator
    {
        [MenuItem("FD/Create/Fire Area Ability")]
        public static void CreateFireAreaAbility()
        {
            // Create the FireAreaAbility
            var ability = ScriptableObject.CreateInstance<FireAreaAbility>();
            ability.abilityName = "Fire Area";
            ability.description = "Every 10 seconds, shoots fire at enemy position. Fire lasts 3 seconds and deals 20 damage per second.";
            
            // Set cooldown to 10 seconds
            ability.cooldownDuration = new ScalableFloat(10f);
            
            // Load burning effect reference
            var burningEffect = AssetDatabase.LoadAssetAtPath<GameplayEffect>("Assets/Prefabs/Abilities/GE_BurningDamage.asset");
            if (burningEffect != null)
            {
                // We'll need to set this via reflection or make it public
                Debug.Log("Loaded burning effect: " + burningEffect.effectName);
            }
            
            // Load fire area prefab reference
            var fireAreaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Abilities/FireAreaPrefab.prefab");
            if (fireAreaPrefab != null)
            {
                Debug.Log("Loaded fire area prefab");
            }
            
            // Save the asset
            string path = "Assets/Prefabs/Abilities/Ability_FireArea.asset";
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(ability, path);
            
            // Now we need to set the serialized fields
            SerializedObject so = new SerializedObject(ability);
            SerializedProperty burningEffectProp = so.FindProperty("burningEffect");
            SerializedProperty fireAreaPrefabProp = so.FindProperty("fireAreaPrefab");
            SerializedProperty fireDurationProp = so.FindProperty("fireDuration");
            SerializedProperty fireRadiusProp = so.FindProperty("fireRadius");
            
            if (burningEffectProp != null)
            {
                burningEffectProp.objectReferenceValue = burningEffect;
            }
            
            if (fireAreaPrefabProp != null)
            {
                fireAreaPrefabProp.objectReferenceValue = fireAreaPrefab;
            }
            
            if (fireDurationProp != null)
            {
                fireDurationProp.floatValue = 3f;
            }
            
            if (fireRadiusProp != null)
            {
                fireRadiusProp.floatValue = 2f;
            }
            
            so.ApplyModifiedProperties();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the asset
            Selection.activeObject = ability;
            EditorGUIUtility.PingObject(ability);
            
            Debug.Log($"Created FireAreaAbility at: {path}");
            Debug.Log("Burning Effect: " + (burningEffect != null ? "Assigned" : "Not found"));
            Debug.Log("Fire Area Prefab: " + (fireAreaPrefab != null ? "Assigned" : "Not found"));
        }
    }
}
