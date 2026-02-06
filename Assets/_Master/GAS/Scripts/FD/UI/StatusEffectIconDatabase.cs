using System.Collections.Generic;
using UnityEngine;
using GAS;

namespace FD.UI
{
    /// <summary>
    /// ScriptableObject database for mapping GameplayTags to status effect icons
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffectIconDatabase", menuName = "FD/UI/Status Effect Icon Database")]
    public class StatusEffectIconDatabase : ScriptableObject
    {
        [System.Serializable]
        public class IconMapping
        {
            public GameplayTag tag;
            public Sprite icon;
            [TextArea(2, 4)]
            public string description;
        }

        [Header("Icon Mappings")]
        [SerializeField] private List<IconMapping> iconMappings = new List<IconMapping>();

        private Dictionary<GameplayTag, Sprite> iconCache;

        /// <summary>
        /// Get icon sprite for a gameplay tag
        /// </summary>
        public Sprite GetIcon(GameplayTag tag)
        {
            // Build cache if needed
            if (iconCache == null)
            {
                BuildCache();
            }

            if (iconCache.TryGetValue(tag, out Sprite sprite))
            {
                return sprite;
            }

            return null;
        }

        /// <summary>
        /// Build the icon cache dictionary
        /// </summary>
        private void BuildCache()
        {
            iconCache = new Dictionary<GameplayTag, Sprite>();

            foreach (var mapping in iconMappings)
            {
                if (mapping.icon != null && !iconCache.ContainsKey(mapping.tag))
                {
                    iconCache[mapping.tag] = mapping.icon;
                }
            }
        }

        /// <summary>
        /// Clear and rebuild cache (call in editor when mappings change)
        /// </summary>
        public void RebuildCache()
        {
            iconCache = null;
            BuildCache();
        }

        private void OnValidate()
        {
            // Clear cache when modified in editor
            iconCache = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper to add all enum values
        /// </summary>
        [ContextMenu("Add All Tags")]
        private void AddAllTags()
        {
            var allTags = System.Enum.GetValues(typeof(GameplayTag));
            
            foreach (GameplayTag tag in allTags)
            {
                if (tag == GameplayTag.None)
                    continue;

                // Check if already exists
                bool exists = false;
                foreach (var mapping in iconMappings)
                {
                    if (mapping.tag == tag)
                    {
                        exists = true;
                        break;
                    }
                }

                // Add if not exists
                if (!exists)
                {
                    iconMappings.Add(new IconMapping
                    {
                        tag = tag,
                        icon = null,
                        description = $"Icon for {tag}"
                    });
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Remove Missing Tags")]
        private void RemoveMissingTags()
        {
            iconMappings.RemoveAll(m => m.icon == null);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
