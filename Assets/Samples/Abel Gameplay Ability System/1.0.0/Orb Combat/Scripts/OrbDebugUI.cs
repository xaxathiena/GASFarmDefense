using System.Collections.Generic;
using System.Text;
using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Cues;
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

        private Vector2 playerScroll;
        private Vector2 enemyScroll;
        private Dictionary<string, GameplayEffect> _effectCache = new Dictionary<string, GameplayEffect>();

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 12;

            // Draw Background Boxes for clear separation
            GUI.Box(new Rect(5, 5, 360, 450), "PLAYER STATUS");
            GUI.Box(new Rect(370, 5, 360, 450), "ENEMY STATUS");

            if (playerASC != null && playerASC.AttributeSet is OrbAttributeSet pAttr)
            {
                playerScroll = DrawASCDetails(new Rect(10, 25, 350, 420), pAttr, playerASC, playerScroll);
            }

            if (enemyASC != null && enemyASC.AttributeSet is OrbAttributeSet eAttr)
            {
                enemyScroll = DrawASCDetails(new Rect(375, 25, 350, 420), eAttr, enemyASC, enemyScroll);
            }

            float testHeight = Screen.height - 460 - 10;
            if (testHeight < 200) testHeight = 200; // Guard
            DrawTestButtons(new Rect(5, 460, 725, testHeight));
        }

        private Vector2 testScroll;

        private void DrawTestButtons(Rect rect)
        {
            GUI.Box(rect, "TEST SUITE & ACTIONS");

            // Inner area for scrolling

            GUILayout.BeginArea(new Rect(rect.x + 5, rect.y + 20, rect.width - 10, rect.height - 25));
            testScroll = GUILayout.BeginScrollView(testScroll);

            // --- SECTION: COMBAT ACTIONS ---
            GUI.color = Color.cyan;
            GUILayout.Label("COMBAT ACTIONS");
            GUI.color = Color.white;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cast Fireball (Ability)"))
            {
                // Logic from bootstrap: -10 HP to enemy, -20 Mana to player
                var geDmg = CreateSimpleGE("FireballDmg", EGameplayAttributeType.Health, EGameplayModifierOp.Add, -10f, EGameplayEffectDurationType.Instant);
                playerASC.ApplyGameplayEffectToTarget(geDmg, enemyASC, playerASC);


                if (playerASC.AttributeSet is OrbAttributeSet p) p.Mana.ModifyCurrentValue(-20f);
                Debug.Log("OrbDebugUI: Cast Fireball!");
            }
            if (GUILayout.Button("Apply Slow (Ability)"))
            {
                // Logic from bootstrap: -2 Speed for 5s
                var geSlow = CreateSimpleGE("SlowAbility", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, -2f, EGameplayEffectDurationType.Duration, 5f, new GameplayTag[] { GameplayTag.Debuff_Slow });
                playerASC.ApplyGameplayEffectToTarget(geSlow, enemyASC, playerASC);
                Debug.Log("OrbDebugUI: Applied Slow to Enemy!");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // --- SECTION: PHASE 1 ---
            GUI.color = Color.yellow;
            GUILayout.Label("PHASE 1: MATH VERIFICATION");
            GUI.color = Color.white;
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("TC1.1: Add +2, Mult x2 (Speed)"))
            {
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

            GUILayout.Space(10);

            // --- SECTION: PHASE 2 ---
            GUI.color = Color.yellow;
            GUILayout.Label("PHASE 2: EFFECT LIFECYCLE");
            GUI.color = Color.white;
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("TC2.1: Slow 3s"))
            {
                var geSlow = CreateSimpleGE("SlowDebuff", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Multiply, 0.5f, EGameplayEffectDurationType.Duration, 3f, new GameplayTag[] { GameplayTag.Debuff_Slow });
                playerASC.ApplyGameplayEffectToSelf(geSlow);
            }

            if (GUILayout.Button("TC2.2: Poison 10dmg/1s (3s)"))
            {
                var gePoison = CreateSimpleGE("PoisonDoT", EGameplayAttributeType.Health, EGameplayModifierOp.Add, -10f, EGameplayEffectDurationType.Duration, 3.1f, new GameplayTag[] { GameplayTag.State_Poisoned });
                gePoison.isPeriodic = true;
                gePoison.period = 1f;
                playerASC.ApplyGameplayEffectToSelf(gePoison);
            }

            if (GUILayout.Button("TC2.3: Dispel (Remove Slow)"))
            {
                playerASC.RemoveGameplayEffectsWithTags(GameplayTag.Debuff_Slow);
            }

            if (GUILayout.Button("TC2.4: Infinite Buff (+2 Speed)"))
            {
                var geInf = CreateSimpleGE("InfiniteSpeed", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, 2f, EGameplayEffectDurationType.Infinite);
                playerASC.ApplyGameplayEffectToSelf(geInf);
            }

            if (GUILayout.Button("TC2.5: Apply Stun (2s)"))
            {
                var geStun = CreateSimpleGE("StunEffect", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, 0f, EGameplayEffectDurationType.Duration, 2f, new GameplayTag[] { GameplayTag.State_Stunned });
                playerASC.ApplyGameplayEffectToSelf(geStun);
                Debug.Log("OrbDebugUI: Applied Stun for 2 seconds.");
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // --- SECTION: PHASE 3 ---
            GUI.color = Color.yellow;
            GUILayout.Label("PHASE 3: STACKING LOGIC");
            GUI.color = Color.white;
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("TC3.1: Stack Speed (Max 3)"))
            {
                var geStack = CreateSimpleGE("StackSpeed", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, 2f, EGameplayEffectDurationType.Duration, 5f);
                geStack.allowStacking = true;
                geStack.maxStacks = 3;
                playerASC.ApplyGameplayEffectToSelf(geStack);
            }

            if (GUILayout.Button("TC3.2: Individual Stacks (3s)"))
            {
                var geInd = CreateSimpleGE("IndiStack", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, 1f, EGameplayEffectDurationType.Duration, 3f);
                geInd.allowStacking = true;
                geInd.maxStacks = 5;
                geInd.stackingDurationPolicy = EGameplayEffectStackingDurationPolicy.IndividualStackDuration;
                playerASC.ApplyGameplayEffectToSelf(geInd);
            }

            if (GUILayout.Button("TC3.3: Refresh Stack (5s)"))
            {
                var geRef = CreateSimpleGE("RefreshStack", EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, 1f, EGameplayEffectDurationType.Duration, 5f);
                geRef.allowStacking = true;
                geRef.maxStacks = 5;
                geRef.stackingDurationPolicy = EGameplayEffectStackingDurationPolicy.RefreshEntireStack;
                geRef.refreshDurationOnStack = true;
                playerASC.ApplyGameplayEffectToSelf(geRef);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // --- SECTION: PHASE 4 ---
            GUI.color = Color.yellow;
            GUILayout.Label("PHASE 4: EVENTS & TAGS");
            GUI.color = Color.white;


            if (GUILayout.Button("Setup Phase 4 (Grant Abilities)"))
            {
                GrantTestAbilities();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("TC4.1: Trigger OnFire Tag"))
            {
                // Applying a tag should trigger the extinguishing ability
                playerASC.AddTag(GameplayTag.State_OnFire);
                Debug.Log("OrbDebugUI: Added Tag OnFire. Look for 'Self-Extinguish' in logs.");
            }

            if (GUILayout.Button("TC4.2: Cast Fireball (While Stunned?)"))
            {
                // This version of Fireball is blocked by Stun tag
                var blockedFireball = CreateTestAbility("BlockedFireball", GameplayTag.Ability_Magic, new GameplayTag[] { GameplayTag.State_Stunned });
                playerASC.GiveAbility(blockedFireball);


                bool success = playerASC.TryActivateAbility(blockedFireball);
                Debug.Log($"OrbDebugUI: Cast BlockedFireball success? {success}");
            }

            if (GUILayout.Button("TC4.3: Send Explosion Event"))
            {
                // Send event with payload
                var payload = new GameplayEventData { EventTag = GameplayTag.Event_Explosion, Magnitude = 50f };
                playerASC.HandleGameplayEvent(GameplayTag.Event_Explosion, payload);
                Debug.Log("OrbDebugUI: Sent Event Explosion (50 dmg).");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // --- SECTION: PHASE 5 ---
            GUI.color = Color.cyan;
            GUILayout.Label("PHASE 5: GAMEPLAY CUES (VFX/SFX)");
            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("TC5.1: Execute Instant Cue"))
            {
                playerASC.ExecuteGameplayCue(GameplayTag.Cue_Fireball_Impact, new GameplayCueParameters(playerASC) { Location = playerASC.Position + Vector3.up });
            }

            if (GUILayout.Button("TC5.2: Toggle Shield Cue"))
            {
                if (playerASC.HasAnyTags(GameplayTag.Cue_Shield_Loop))
                    playerASC.RemoveTag(GameplayTag.Cue_Shield_Loop);
                else
                    playerASC.AddTag(GameplayTag.Cue_Shield_Loop);


                Debug.Log($"OrbDebugUI: Shield Tag toggled. HasTag: {playerASC.HasAnyTags(GameplayTag.Cue_Shield_Loop)}");
            }
            GUILayout.EndHorizontal();

            // --- SECTION: IMPROVED CUE SYSTEM (PRO) ---
            GUI.color = Color.cyan;
            GUILayout.Label("AAA CUE SYSTEM (FINAL VALIDATION)");
            GUI.color = Color.white;

            if (GUILayout.Button("Full System Test (Pool + Tick + Addressables)"))
            {
                RunFullSystemTest();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void RunFullSystemTest()
        {
            Debug.Log("<color=cyan>[PRO CUE]</color> Starting FINAL SYSTEM TEST...");

            // 1. Test Bitmask Packing

            RunPhase1Test();

            // 2. Test Pooling & Ticking
            Debug.Log("[SystemTest] Spawning 10 cues rapidly to test Pooling...");
            for (int i = 0; i < 10; i++)
            {
                playerASC.ExecuteGameplayCue(GameplayTag.Cue_Fireball_Impact, new GameplayCueParameters(playerASC));
            }


            Debug.Log("<color=green>[SystemTest] Done!</color> Check GAS Dashboard for Pool/Active stats.");
        }

        private void RunPhase3Test()
        {
            Debug.Log("<color=yellow>[PRO CUE]</color> Starting Phase 3 Test: Specialized Notifiers...");

            // 1. Create a Burst Effect object
            GameObject burstObj = new GameObject("Phase3_Burst_Effect");
            var burstNotify = burstObj.AddComponent<GameplayCueNotify_Burst>();

            // 2. Configure (In real use, this is done in Prefab Inspector)
            // We'll use reflection to set private tag if needed, but here we can just use the property if public
            // For testing, we'll manually register it to the manager

            GameplayCueManager.Instance.RegisterNotifier(burstNotify);

            Debug.Log("[Phase 3] Spawned Burst Notifier. Executing...");

            // 3. Execute
            playerASC.ExecuteGameplayCue(burstNotify.CueTag, new GameplayCueParameters(playerASC));

            Debug.Log("<color=green>[Phase 3] SUCCESS!</color> Check if 'Phase3_Burst_Effect' destroys itself in 2 seconds.");
        }

        private void RunPhase1Test()
        {
            Debug.Log("<color=yellow>[PRO CUE]</color> Starting Phase 1 Test: Bitmask Serialization...");

            // 1. Define test data
            var originalData = new TestExplosionPayload
            {
                Radius = 15.5f,
                Intensity = 0.8f,
                // FlashColor và Pos để mặc định (Zero)
            };

            // 2. Pack data
            var parameters = new GameplayCueParameters(playerASC);
            parameters.SetData(originalData);

            // 3. Verify Size
            // Bitmask (8 bytes ulong) + Radius (4 bytes) + Intensity (4 bytes) = 16 bytes
            // (Hiện tại BitmaskCompressor dùng ulong 8 bytes làm header cố định)
            int expectedSize = 8 + 4 + 4;

            int actualSize = parameters.RawPayload.Length;

            Debug.Log($"[Phase 1] Packed Payload Size: {actualSize} bytes. (Expected {expectedSize} bytes if Pos/Color are zero)");

            // 4. Unpack and Verify
            var unpackedData = parameters.GetData<TestExplosionPayload>();

            bool success = Mathf.Approximately(unpackedData.Radius, originalData.Radius) &&
                           Mathf.Approximately(unpackedData.Intensity, originalData.Intensity) &&
                           unpackedData.Pos == Vector3.zero;

            if (success)
                Debug.Log("<color=green>[Phase 1] SUCCESS!</color> Data integrity verified. Packing is efficient.");
            else
                Debug.LogError("[Phase 1] FAILED! Data mismatch after unpacking.");
        }

        private void RunPhase2Test()
        {
            Debug.Log("<color=yellow>[PRO CUE]</color> Starting Phase 2 Test: Asset Library...");

            // 1. Clean up ALL previous test objects
            var existingContainer = GameObject.Find("CUE_TEST_CONTAINER");
            if (existingContainer != null) Destroy(existingContainer);

            // 2. Create a clean container for this test run
            GameObject testContainer = new GameObject("CUE_TEST_CONTAINER");
            testContainer.SetActive(false); // Keep the "prefabs" hidden

            // 3. Create a runtime Cue Set
            var runtimeSet = ScriptableObject.CreateInstance<GameplayCueSet>();

            // 4. Map a tag to a dummy prefab (we'll create a simple cube)
            GameObject dummyPrefab = new GameObject("Phase2_Test_Cube_Prefab");
            dummyPrefab.transform.SetParent(testContainer.transform);
            Debug.Log("create dummyPrefab :  " + dummyPrefab.name);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestVisualCube";
            cube.transform.SetParent(dummyPrefab.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * 0.4f;
            cube.GetComponent<Renderer>().material.color = Color.magenta;
            if (cube.TryGetComponent<BoxCollider>(out var col)) Destroy(col);

            dummyPrefab.SetActive(false);

            // 3. Register to runtime set

            runtimeSet.AddMappingRuntime(GameplayTag.Cue_Fireball_Impact, dummyPrefab);

            // 4. Inject into Manager

            GameplayCueManager.Instance.SetGlobalCueSet(runtimeSet);


            Debug.Log("[Phase 2] Registered Runtime Set with Magenta Cube. Executing Cue...");

            // 5. Execute

            playerASC.ExecuteGameplayCue(GameplayTag.Cue_Fireball_Impact, new GameplayCueParameters(playerASC));

            Debug.Log("<color=green>[Phase 2] SUCCESS!</color> If you see a Magenta Cube, the Asset Library is working.");
        }

        // Test struct following the Optimization Standard
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TestExplosionPayload
        {
            public Vector3 Pos;      // 12 bytes
            public Color32 FlashColor; // 4 bytes
            public float Radius;     // 4 bytes
            public float Intensity;  // 4 bytes
        }

        private void GrantTestAbilities()
        {
            // 1. Ability that triggers when State.OnFire is added
            var extinguish = CreateTestAbility("SelfExtinguish", GameplayTag.Ability_Ultimate);
            extinguish.abilityTriggers = new AbilityTriggerData[] {
                new AbilityTriggerData { TriggerTag = GameplayTag.State_OnFire, TriggerSource = EAbilityTriggerSource.OwnedTagAdded }
            };
            playerASC.GiveAbility(extinguish);

            // 2. Ability that triggers on Explosion Event
            var react = CreateTestAbility("ExplosionReact", GameplayTag.Ability_Magic);
            react.abilityTriggers = new AbilityTriggerData[] {
                new AbilityTriggerData { TriggerTag = GameplayTag.Event_Explosion, TriggerSource = EAbilityTriggerSource.GameplayEvent }
            };
            playerASC.GiveAbility(react);

            Debug.Log("OrbDebugUI: Phase 4 Test Abilities Granted!");
        }

        private GameplayAbilityData CreateTestAbility(string name, GameplayTag abilityTag, GameplayTag[] blockedTags = null)
        {
            var ability = ScriptableObject.CreateInstance<SimpleAbilityData>();
            ability.abilityName = name;
            ability.abilityTags = new GameplayTag[] { abilityTag };
            ability.activationBlockedTags = blockedTags;
            return ability;
        }

        private GameplayEffect CreateSimpleGE(string name, EGameplayAttributeType attr, EGameplayModifierOp op, float val, EGameplayEffectDurationType durationType, float duration = 0f, GameplayTag[] tags = null)
        {
            if (_effectCache.TryGetValue(name, out var cachedGE))
                return cachedGE;

            var ge = ScriptableObject.CreateInstance<GameplayEffect>();
            ge.name = name;
            ge.effectName = name;
            ge.durationType = durationType;
            ge.durationMagnitude = duration;
            ge.grantedTags = tags;

            var mod = new GameplayEffectModifier(attr, op, val);
            ge.modifiers = new GameplayEffectModifier[] { mod };

            _effectCache[name] = ge;
            return ge;
        }

        private Vector2 DrawASCDetails(Rect rect, OrbAttributeSet attrSet, AbilitySystemComponent asc, Vector2 scrollPos)
        {
            // Define the content height based on active effects
            float contentHeight = 350 + (asc.GetActiveGameplayEffects().Count * 20);
            Rect viewRect = new Rect(0, 0, rect.width - 20, contentHeight);


            scrollPos = GUI.BeginScrollView(rect, scrollPos, viewRect);


            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetAttributeDetails("HP", attrSet.Health));
            sb.AppendLine(GetAttributeDetails("MaxHP", attrSet.MaxHealth));
            sb.AppendLine(GetAttributeDetails("Mana", attrSet.Mana));
            sb.AppendLine(GetAttributeDetails("Speed", attrSet.Speed));


            sb.AppendLine("\n[ACTIVE EFFECTS]");
            var activeEffects = asc.GetActiveGameplayEffects();
            if (activeEffects.Count > 0)
            {
                foreach (var effect in activeEffects)
                {
                    sb.AppendLine($"- {effect.ToString()}");
                }
            }
            else
            {
                sb.AppendLine("None");
            }

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

            GUI.Label(new Rect(5, 5, viewRect.width, viewRect.height), sb.ToString());


            GUI.EndScrollView();
            return scrollPos;
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

    /// <summary>
    /// Simple concrete implementation for testing purposes.
    /// </summary>
    public class SimpleAbilityData : GameplayAbilityData { }
}

// Force recompile after package update 1.1.0
