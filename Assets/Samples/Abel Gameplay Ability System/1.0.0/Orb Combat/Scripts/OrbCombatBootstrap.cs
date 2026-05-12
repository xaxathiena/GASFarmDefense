using Abel.GAS;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Abel.GAS.Samples.OrbCombat
{
    /// <summary>
    /// Mini-game bootstrap to verify decoupled GAS logic.
    /// Manages player/enemy creation and provides a test UI.
    /// </summary>
    public class OrbCombatBootstrap : MonoBehaviour
    {
        [SerializeField] private OrbCombatLifetimeScope _lifetimeScope;

        private AbilitySystemComponent _playerASC;
        private AbilitySystemComponent _enemyASC;

        private void Start()
        {
            if (_lifetimeScope == null)
            {
                _lifetimeScope = FindObjectOfType<OrbCombatLifetimeScope>();
            }

            if (_lifetimeScope == null)
            {
                Debug.LogError("[OrbCombat] Missing LifetimeScope! Please setup the scene correctly.");
                return;
            }

            // Resolve dependencies from scope
            var container = _lifetimeScope.Container;

            // 1. Setup Entities

            // Player

            GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerObj.name = "PlayerOrb";
            playerObj.transform.position = new Vector3(-2, 0, 0);
            playerObj.GetComponent<Renderer>().material.color = Color.blue;

            _playerASC = container.Resolve<AbilitySystemComponent>();
            _playerASC.InitializeAttributeSet(new OrbAttributeSet());
            _playerASC.UnitInstanceID = playerObj.GetInstanceID();

            // Enemy
            GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            enemyObj.name = "EnemyOrb";
            enemyObj.transform.position = new Vector3(2, 0, 0);
            enemyObj.GetComponent<Renderer>().material.color = Color.red;

            _enemyASC = container.Resolve<AbilitySystemComponent>();
            _enemyASC.InitializeAttributeSet(new OrbAttributeSet());
            _enemyASC.UnitInstanceID = enemyObj.GetInstanceID();

            // 2. Setup UI
            SetupDebugUI();

            Debug.Log("[OrbCombat] Bootstrap Complete. Player (Blue) vs Enemy (Red)");
        }

        private void Update()
        {
            // IMPORTANT: GAS Core requires manual ticking of ASCs
            _playerASC?.Tick();
            _enemyASC?.Tick();
        }

        private void SetupDebugUI()
        {
            GameObject canvasObj = new GameObject("OrbCombatUI");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject uiObj = new GameObject("DebugPanel");
            uiObj.transform.SetParent(canvasObj.transform);

            OrbDebugUI debugUI = uiObj.AddComponent<OrbDebugUI>();
            debugUI.playerASC = _playerASC;
            debugUI.enemyASC = _enemyASC;
        }

        private void OnGUI()
        {
            if (_playerASC == null || _enemyASC == null) return;

            GUI.Box(new Rect(10, 10, 250, 100), "Orb Combat Controller");

            if (GUI.Button(new Rect(20, 40, 100, 30), "Cast Fireball"))
            {
                ExecuteFireball();
            }

            if (GUI.Button(new Rect(130, 40, 100, 30), "Apply Slow"))
            {
                ExecuteSlow();
            }
        }

        private void ExecuteFireball()
        {
            // Create an instant damage effect
            var damageEffect = ScriptableObject.CreateInstance<GameplayEffect>();
            damageEffect.name = "InstantDamageEffect";
            damageEffect.durationType = EGameplayEffectDurationType.Instant;

            // Modifier: Reduce Health by 10

            damageEffect.modifiers = new GameplayEffectModifier[] {
                new GameplayEffectModifier(EGameplayAttributeType.Health, EGameplayModifierOp.Add, -10f)
            };

            // Apply to enemy
            _playerASC.ApplyGameplayEffectToTarget(damageEffect, _enemyASC, _playerASC);

            // Deduct Mana directly

            var pAttr = _playerASC.AttributeSet as OrbAttributeSet;
            if (pAttr != null) pAttr.Mana.ModifyCurrentValue(-20f);


            Debug.Log("[OrbCombat] Fireball Casted! Enemy -10 HP, Player -20 Mana");
        }

        private void ExecuteSlow()
        {
            // Create a duration slow effect (5 seconds)
            var slowEffect = ScriptableObject.CreateInstance<GameplayEffect>();
            slowEffect.name = "DurationSlowEffect";
            slowEffect.durationType = EGameplayEffectDurationType.Duration;
            slowEffect.durationMagnitude = 5f;

            // Modifier: Reduce MoveSpeed by 2

            slowEffect.modifiers = new GameplayEffectModifier[] {
                new GameplayEffectModifier(EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Add, -2f)
            };

            // Apply to enemy
            _playerASC.ApplyGameplayEffectToTarget(slowEffect, _enemyASC, _playerASC);
            Debug.Log("[OrbCombat] Slow Applied to Enemy for 5 seconds!");
        }
    }
}
