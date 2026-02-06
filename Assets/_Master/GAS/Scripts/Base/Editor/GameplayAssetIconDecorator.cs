#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GAS.Editor
{
    /// <summary>
    /// Adds small colored badges to GameplayAbility and GameplayEffect assets in the Project window
    /// so they are easy to distinguish at a glance.
    /// </summary>
    [InitializeOnLoad]
    public static class GameplayAssetIconDecorator
    {
        private static readonly Color AbilityColor = new Color(0.18f, 0.55f, 0.96f, 0.9f);
        private static readonly Color EffectColor = new Color(0.9f, 0.35f, 0.35f, 0.9f);
        private const string AbilityLabel = "GA";
        private const string EffectLabel = "GE";

        static GameplayAssetIconDecorator()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (assetType == null)
            {
                return;
            }

            if (IsGameplayAbilityType(assetType))
            {
                DrawBadge(selectionRect, AbilityColor, AbilityLabel);
            }
            else if (IsGameplayEffectType(assetType))
            {
                DrawBadge(selectionRect, EffectColor, EffectLabel);
            }
        }

        private static bool IsGameplayAbilityType(System.Type type)
        {
            return typeof(GameplayAbility).IsAssignableFrom(type);
        }

        private static bool IsGameplayEffectType(System.Type type)
        {
            return typeof(GameplayEffect).IsAssignableFrom(type);
        }

        private static void DrawBadge(Rect selectionRect, Color color, string label)
        {
            const float size = 18f;
            var badgeRect = new Rect(selectionRect.xMax - size - 2f, selectionRect.yMin + 2f, size, size);
            EditorGUI.DrawRect(badgeRect, color);

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 9
            };

            GUI.Label(badgeRect, label, labelStyle);
        }
    }
}
#endif
