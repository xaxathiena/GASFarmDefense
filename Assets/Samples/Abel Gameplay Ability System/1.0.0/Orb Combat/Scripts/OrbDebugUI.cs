using UnityEngine;
using Abel.GAS;

namespace Abel.GAS.Samples.OrbCombat
{
    /// <summary>
    /// Displays GAS stats using OnGUI to avoid dependency on specific UI systems in the sample.
    /// </summary>
    public class OrbDebugUI : MonoBehaviour
    {
        public AbilitySystemComponent playerASC;
        public AbilitySystemComponent enemyASC;

        private void OnGUI()
        {
            if (playerASC != null && playerASC.AttributeSet is OrbAttributeSet pAttr)
            {
                string text = $"[PLAYER]\nHP: {pAttr.Health.CurrentValue:F1}/{pAttr.MaxHealth.CurrentValue:F1}\n" +
                              $"Mana: {pAttr.Mana.CurrentValue:F1}\n" +
                              $"Speed: {pAttr.Speed.CurrentValue:F1}";
                GUI.Label(new Rect(10, 120, 250, 100), text);
            }

            if (enemyASC != null && enemyASC.AttributeSet is OrbAttributeSet eAttr)
            {
                string text = $"[ENEMY]\nHP: {eAttr.Health.CurrentValue:F1}/{eAttr.MaxHealth.CurrentValue:F1}";
                GUI.Label(new Rect(300, 120, 250, 100), text);
            }

            // Display Active Tags
            if (playerASC != null)
            {
                var tags = playerASC.GetActiveTags();
                if (tags.Count > 0)
                {
                    GUI.Label(new Rect(10, 200, 250, 100), "Player Tags: " + string.Join(", ", tags));
                }
            }
        }
    }
}
