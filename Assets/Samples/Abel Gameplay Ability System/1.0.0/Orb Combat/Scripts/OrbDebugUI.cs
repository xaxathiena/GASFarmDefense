using System.Collections.Generic;
using System.Text;
using Abel.GAS;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
using UnityEngine;

namespace Abel.GAS.Samples.OrbCombat
{
    /// <summary>
    /// Displays GAS stats using OnGUI to avoid dependency on specific UI systems in the sample.
    /// Enhanced for Phase 1 verification.
    /// </summary>
    public class OrbDebugUI : MonoBehaviour
    {
        public AbilitySystemComponent playerASC;
        public AbilitySystemComponent enemyASC;

        private void OnGUI()
        {
            // Set a larger font for better readability
            GUI.skin.label.fontSize = 12;

            if (playerASC != null && playerASC.AttributeSet is OrbAttributeSet pAttr)
            {
                DrawASCDetails(new Rect(10, 50, 350, 400), "[PLAYER]", pAttr, playerASC);
            }

            if (enemyASC != null && enemyASC.AttributeSet is OrbAttributeSet eAttr)
            {
                DrawASCDetails(new Rect(400, 50, 350, 400), "[ENEMY]", eAttr, enemyASC);
            }

            DrawTestButtons(new Rect(10, 460, 740, 100));
        }

        private void DrawTestButtons(Rect rect)
        {
            GUI.Box(rect, "PHASE 1: MATH VERIFICATION");
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 20, rect.width - 20, rect.height - 30));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("TC1.1: Add +2, Mult x2 (Speed)"))
            {
                // Create temp effects
                var geAdd = CreateSimpleGE("TestAdd", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, 2f, EGameplayEffectDurationType.Duration, 5f);
                var geMult = CreateSimpleGE("TestMult", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Multiply, 2f, EGameplayEffectDurationType.Duration, 5f);
                playerASC.ApplyGameplayEffectToSelf(geAdd);
                playerASC.ApplyGameplayEffectToSelf(geMult);
            }

            if (GUILayout.Button("TC1.2: Override=10 (Speed)"))
            {
                var geOverride = CreateSimpleGE("TestOverride", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Override, 10f, EGameplayEffectDurationType.Duration, 5f);
                playerASC.ApplyGameplayEffectToSelf(geOverride);
            }

            if (GUILayout.Button("TC1.3: Heal +50 (HP Clamping)"))
            {
                var geHeal = CreateSimpleGE("TestHeal", EGameplayAttributeType.Health, EGameplayModifierOp.Add, 50f, EGameplayEffectDurationType.Instant);
                playerASC.ApplyGameplayEffectToSelf(geHeal);
            }

            if (GUILayout.Button("TC1.4: Damage -200 (HP Min)"))
            {
                var geDmg = CreateSimpleGE("TestDmg", EGameplayAttributeType.Health, EGameplayModifierOp.Add, -200f, EGameplayEffectDurationType.Instant);
                playerASC.ApplyGameplayEffectToSelf(geDmg);
            }

            if (GUILayout.Button("RESET ALL"))
            {
                playerASC.RemoveAllGameplayEffects();
                if (playerASC.AttributeSet is OrbAttributeSet p)
                {
                    p.Health.SetBaseValue(100f);
                    p.Mana.SetBaseValue(50f);
                    p.Speed.SetBaseValue(5f);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private GameplayEffect CreateSimpleGE(string name, EGameplayAttributeType attr, EGameplayModifierOp op, float val, EGameplayEffectDurationType durationType, float duration = 0f)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffect>();
            ge.effectName = name;
            ge.durationType = durationType;
            ge.durationMagnitude = duration;


            var mod = new GameplayEffectModifier(attr, op, val);
            ge.modifiers = new GameplayEffectModifier[] { mod };
            return ge;
        }

        private void DrawASCDetails(Rect rect, string header, OrbAttributeSet attrSet, AbilitySystemComponent asc)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(header);
            sb.AppendLine(GetAttributeDetails("HP", attrSet.Health));
            sb.AppendLine(GetAttributeDetails("MaxHP", attrSet.MaxHealth));
            sb.AppendLine(GetAttributeDetails("Mana", attrSet.Mana));
            sb.AppendLine(GetAttributeDetails("Speed", attrSet.Speed));


            sb.AppendLine("\n[ACTIVE TAGS]");
            var tags = asc.GetActiveTags();
            if (tags.Count > 0)
            {
                sb.AppendLine(string.Join(", ", tags));
            }
            else
            {
                sb.AppendLine("None");
            }

            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 10, rect.y + 5, rect.width - 20, rect.height - 10), sb.ToString());
        }

        private string GetAttributeDetails(string name, GameplayAttribute attr)
        {
            var modifiers = attr.GetActiveModifiers();
            StringBuilder sb = new StringBuilder();
            sb.Append($"{name}: {attr.CurrentValue:F2} (Base: {attr.BaseValue:F1})");


            if (modifiers.Count > 0)
            {
                sb.Append(" [Mods: ");
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var mod = modifiers[i];
                    string opSign = mod.Operation switch
                    {
                        EGameplayModifierOp.Add => "+",
                        EGameplayModifierOp.Multiply => "x",
                        EGameplayModifierOp.Divide => "/",
                        EGameplayModifierOp.Override => "=",
                        _ => "?"
                    };
                    sb.Append($"{opSign}{mod.Magnitude:F1}");
                    if (i < modifiers.Count - 1) sb.Append(", ");
                }
                sb.Append("]");
            }


            return sb.ToString();
        }
    }
}
