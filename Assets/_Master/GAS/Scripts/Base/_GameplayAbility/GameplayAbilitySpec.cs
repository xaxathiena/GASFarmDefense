using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Runtime wrapper that tracks per-owner ability level and active state.
    /// </summary>
    [System.Serializable]
    public class GameplayAbilitySpec
    {
        public GameplayAbilityData Definition => definition;
        public float Level => level;
        public bool IsActive => isActive;

        private readonly GameplayAbilityData definition;
        private float level;
        private bool isActive;

        public GameplayAbilitySpec(GameplayAbilityData definition, float level)
        {
            this.definition = definition;
            SetLevel(level);
        }

        public void SetLevel(float newLevel)
        {
            level = Mathf.Max(1f, newLevel);
        }

        public void AddLevels(float delta)
        {
            SetLevel(level + delta);
        }

        internal void SetActiveState(bool active)
        {
            isActive = active;
        }
    }
}
