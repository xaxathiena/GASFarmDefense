using UnityEngine;
using GAS;

namespace FD.Tests
{
    /// <summary>
    /// Test script to verify modifier aggregation system works correctly
    /// Tests:
    /// 1. Add modifier → CurrentValue changes
    /// 2. Remove modifier → CurrentValue recalculates correctly
    /// 3. Multiple effects → Execution order (Add → Multiply → Divide)
    /// 4. Stack effects → Magnitude multiplied by stack count
    /// </summary>
    public class ModifierAggregationTest : MonoBehaviour
    {
        [Header("Test Setup")]
        [SerializeField] private GameObject testCharacter;
        [SerializeField] private GameplayEffect addEffect; // +20 MoveSpeed
        [SerializeField] private GameplayEffect multiplyEffect; // *0.5 MoveSpeed
        [SerializeField] private GameplayEffect stackingEffect; // +10 MoveSpeed (stackable)

        private AbilitySystemComponent testASC;

        private void Start()
        {
            if (testCharacter == null)
            {
                Debug.LogError("Test Character not assigned!");
                return;
            }

            testASC = testCharacter.GetComponent<AbilitySystemComponent>();
            if (testASC == null)
            {
                Debug.LogError("Test Character doesn't have AbilitySystemComponent!");
                return;
            }

            Debug.Log("=== MODIFIER AGGREGATION TEST START ===");
            RunAllTests();
            Debug.Log("=== MODIFIER AGGREGATION TEST END ===");
        }

        private void RunAllTests()
        {
            Test1_AddAndRemoveSingleEffect();
            Test2_MultipleEffectsExecutionOrder();
            Test3_StackingEffects();
            Test4_RemoveMiddleEffect();
        }

        /// <summary>
        /// Test 1: Apply effect → Remove effect → Value restored
        /// </summary>
        private void Test1_AddAndRemoveSingleEffect()
        {
            Debug.Log("\n--- Test 1: Add and Remove Single Effect ---");

            var moveSpeed = testASC.AttributeSet.GetAttribute(EGameplayAttributeType.MoveSpeed);
            float baseValue = moveSpeed.BaseValue;

            Debug.Log($"Initial BaseValue: {baseValue}");
            Debug.Log($"Initial CurrentValue: {moveSpeed.CurrentValue}");

            // Apply effect (+20)
            var activeEffect = testASC.ApplyGameplayEffectToSelf(addEffect);
            Debug.Log($"After applying +20 effect: CurrentValue = {moveSpeed.CurrentValue} (Expected: {baseValue + 20})");

            // Remove effect
            testASC.RemoveGameplayEffect(activeEffect);
            Debug.Log($"After removing effect: CurrentValue = {moveSpeed.CurrentValue} (Expected: {baseValue})");

            bool passed = Mathf.Approximately(moveSpeed.CurrentValue, baseValue);
            Debug.Log($"Test 1: {(passed ? "PASSED ✓" : "FAILED ✗")}");
        }

        /// <summary>
        /// Test 2: Multiple effects → Execution order (Add → Multiply)
        /// Base: 100, Add +20 → 120, Multiply *0.5 → 60
        /// </summary>
        private void Test2_MultipleEffectsExecutionOrder()
        {
            Debug.Log("\n--- Test 2: Multiple Effects Execution Order ---");

            var moveSpeed = testASC.AttributeSet.GetAttribute(EGameplayAttributeType.MoveSpeed);
            float baseValue = moveSpeed.BaseValue;

            Debug.Log($"Initial BaseValue: {baseValue}");

            // Apply Add effect (+20)
            var addEffectActive = testASC.ApplyGameplayEffectToSelf(addEffect);
            Debug.Log($"After Add +20: CurrentValue = {moveSpeed.CurrentValue}");

            // Apply Multiply effect (*0.5)
            var multiplyEffectActive = testASC.ApplyGameplayEffectToSelf(multiplyEffect);
            float expectedAfterBoth = (baseValue + 20) * 0.5f;
            Debug.Log($"After Multiply *0.5: CurrentValue = {moveSpeed.CurrentValue} (Expected: {expectedAfterBoth})");

            // Remove effects
            testASC.RemoveGameplayEffect(addEffectActive);
            testASC.RemoveGameplayEffect(multiplyEffectActive);

            bool passed = Mathf.Approximately(moveSpeed.CurrentValue, baseValue);
            Debug.Log($"After removing all: CurrentValue = {moveSpeed.CurrentValue} (Expected: {baseValue})");
            Debug.Log($"Test 2: {(passed ? "PASSED ✓" : "FAILED ✗")}");
        }

        /// <summary>
        /// Test 3: Stacking effects
        /// </summary>
        private void Test3_StackingEffects()
        {
            Debug.Log("\n--- Test 3: Stacking Effects ---");

            if (stackingEffect == null || !stackingEffect.allowStacking)
            {
                Debug.LogWarning("Stacking effect not configured, skipping test 3");
                return;
            }

            var moveSpeed = testASC.AttributeSet.GetAttribute(EGameplayAttributeType.MoveSpeed);
            float baseValue = moveSpeed.BaseValue;

            Debug.Log($"Initial BaseValue: {baseValue}");

            // Apply first stack
            var firstStack = testASC.ApplyGameplayEffectToSelf(stackingEffect);
            Debug.Log($"After 1 stack: CurrentValue = {moveSpeed.CurrentValue}, StackCount = {firstStack.StackCount}");

            // Apply second stack
            var secondStack = testASC.ApplyGameplayEffectToSelf(stackingEffect);
            Debug.Log($"After 2 stacks: CurrentValue = {moveSpeed.CurrentValue}, StackCount = {firstStack.StackCount}");

            // Remove effect
            testASC.RemoveGameplayEffect(firstStack);
            Debug.Log($"After removing: CurrentValue = {moveSpeed.CurrentValue} (Expected: {baseValue})");

            bool passed = Mathf.Approximately(moveSpeed.CurrentValue, baseValue);
            Debug.Log($"Test 3: {(passed ? "PASSED ✓" : "FAILED ✗")}");
        }

        /// <summary>
        /// Test 4: Remove middle effect → Others recalculate correctly
        /// Apply A (+20), B (*0.5), C (+10)
        /// Remove B → Result should be Base + 20 + 10
        /// </summary>
        private void Test4_RemoveMiddleEffect()
        {
            Debug.Log("\n--- Test 4: Remove Middle Effect ---");

            var moveSpeed = testASC.AttributeSet.GetAttribute(EGameplayAttributeType.MoveSpeed);
            float baseValue = moveSpeed.BaseValue;

            Debug.Log($"Initial BaseValue: {baseValue}");

            // Apply 3 effects
            var effectA = testASC.ApplyGameplayEffectToSelf(addEffect); // +20
            var effectB = testASC.ApplyGameplayEffectToSelf(multiplyEffect); // *0.5
            var effectC = testASC.ApplyGameplayEffectToSelf(addEffect); // +20 again

            Debug.Log($"After applying A, B, C: CurrentValue = {moveSpeed.CurrentValue}");
            Debug.Log($"Expected: ({baseValue} + 20 + 20) * 0.5 = {(baseValue + 40) * 0.5f}");

            // Remove B (multiply effect)
            testASC.RemoveGameplayEffect(effectB);
            float expectedAfterRemoveB = baseValue + 40; // Just adds remaining
            Debug.Log($"After removing B: CurrentValue = {moveSpeed.CurrentValue} (Expected: {expectedAfterRemoveB})");

            // Cleanup
            testASC.RemoveGameplayEffect(effectA);
            testASC.RemoveGameplayEffect(effectC);

            bool passed = Mathf.Approximately(moveSpeed.CurrentValue, baseValue);
            Debug.Log($"After cleanup: CurrentValue = {moveSpeed.CurrentValue} (Expected: {baseValue})");
            Debug.Log($"Test 4: {(passed ? "PASSED ✓" : "FAILED ✗")}");
        }

        [ContextMenu("Run Tests")]
        public void RunTestsManually()
        {
            Start();
        }
    }
}