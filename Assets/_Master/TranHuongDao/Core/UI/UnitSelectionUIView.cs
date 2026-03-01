using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Abel.TowerDefense.Config;

namespace Abel.TranHuongDao.Core
{
    // ─────────────────────────────────────────────────────────────────────────────
    // UnitSelectionUIView
    //
    // Drives the Warcraft 3 style bottom info panel.
    // Pure presentation layer — receives pre-computed data, applies it to widgets.
    // ─────────────────────────────────────────────────────────────────────────────
    public class UnitSelectionUIView : MonoBehaviour
    {
        // ── Serialized fields ────────────────────────────────────────────────────

        [Header("Portrait")]
        [SerializeField] private RawImage portraitImage;

        [Header("Render Database")]
        [Tooltip("Assign the UnitRenderDatabase ScriptableObject. Used to look up portrait animation data.")]
        [SerializeField] private UnitRenderDatabase renderDatabase;

        [Header("Stat Texts")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI armorText;
        [SerializeField] private TextMeshProUGUI tierText;

        [Header("Inventory")]
        [SerializeField] private Transform inventoryGridRoot;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Merge Button")]
        [SerializeField] private Button mergeBtn;

        // ── Runtime state ────────────────────────────────────────────────────────

        private UIPortraitAnimator _portraitAnimator;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // UIPortraitAnimator lives on the same GameObject as the RawImage reference.
            if (portraitImage != null)
                _portraitAnimator = portraitImage.GetComponent<UIPortraitAnimator>();

            // Attempt to auto-find the MergeButton if not assigned.
            if (mergeBtn == null)
            {
                var tr = panelRoot != null ? panelRoot.transform : transform;
                var found = tr.Find("MergeButton");
                if (found != null) mergeBtn = found.GetComponent<Button>();
            }
            if (mergeBtn != null)
                mergeBtn.gameObject.SetActive(false); // Default hidden
        }

        // ── Public property ──────────────────────────────────────────────────────

        /// <summary>Returns true when the panel is currently active and visible.</summary>
        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Populates all stat widgets, starts the portrait animation, and makes the panel visible.
        /// HP and Damage are read from live GAS attributes so buffs/debuffs are
        /// reflected immediately. Static meta (Tier, AttackType) still comes from
        /// the authored config struct.
        /// </summary>
        /// <param name="config">Authored balance snapshot — used for static meta only.</param>
        /// <param name="attributes">Live GAS attribute set — source of truth for all modifiable stats.</param>
        public void ShowUnit(UnitConfig config, UnitAttributeSet attributes)
        {
            // Activate the root panel first so all child widgets are enabled.
            SetPanelActive(true);

            // Hide merge button on fresh selection
            if (mergeBtn != null)
                mergeBtn.gameObject.SetActive(false);

            // Name
            if (nameText != null)
                nameText.text = config.UnitID;

            // HP — live values from GAS; format: "220 / 220"
            if (hpText != null)
            {
                int current = Mathf.CeilToInt(attributes.Health.CurrentValue);
                int max = Mathf.CeilToInt(attributes.MaxHealth.CurrentValue);
                hpText.text = $"{current} / {max}";
            }

            // Damage — dynamic: reflects active buffs/debuffs applied by GameplayEffects
            if (damageText != null)
                damageText.text = Mathf.CeilToInt(attributes.Damage.CurrentValue).ToString();

            // Armor — no dedicated attribute yet; placeholder until the field is added.
            if (armorText != null)
                armorText.text = "0";

            // Tier — static meta from config; GAS never modifies this.
            if (tierText != null)
                tierText.text = $"Tier {config.Tier}";

            // Portrait animation — look up idle clip from render database.
            UpdatePortrait(config.UnitID);
        }

        public void SetMergeButtonActive(bool active, UnityEngine.Events.UnityAction onClickAction = null)
        {
            if (mergeBtn == null) return;

            mergeBtn.gameObject.SetActive(active);
            mergeBtn.onClick.RemoveAllListeners();
            if (active && onClickAction != null)
            {
                mergeBtn.onClick.AddListener(onClickAction);
            }
        }

        /// <summary>Deactivates the panel and stops the portrait animation.</summary>
        public void Hide()
        {
            _portraitAnimator?.Stop();
            SetPanelActive(false);
            if (mergeBtn != null)
                mergeBtn.gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates only the live numeric labels (HP, Damage) without re-triggering
        /// the portrait animation or toggling panel visibility.
        /// Called every frame while a unit is selected.
        /// </summary>
        public void RefreshStats(UnitConfig config, UnitAttributeSet attributes)
        {
            if (hpText != null)
            {
                int current = Mathf.CeilToInt(attributes.Health.CurrentValue);
                int max = Mathf.CeilToInt(attributes.MaxHealth.CurrentValue);
                hpText.text = $"{current} / {max}";
            }

            if (damageText != null)
                damageText.text = Mathf.CeilToInt(attributes.Damage.CurrentValue).ToString();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Looks up the unit's idle animation in the render database and drives
        /// UIPortraitAnimator with the correct Texture2DArray slice range.
        /// </summary>
        private void UpdatePortrait(string unitID)
        {
            if (_portraitAnimator == null || renderDatabase == null)
                return;

            var profile = renderDatabase.GetUnitByID(unitID);
            if (profile?.animData == null)
            {
                _portraitAnimator.Stop();
                return;
            }

            if (profile.animData.GetAnim(UnitAnimState.Idle, out var idleClip))
            {
                float speed = idleClip.speedModifier > 0f ? idleClip.speedModifier : 1f;
                _portraitAnimator.PlayAnimation(
                    profile.animData.textureArray,
                    idleClip.startFrame,
                    idleClip.frameCount,
                    idleClip.fps * speed);
            }
            else if (profile.animData.animations.Count > 0)
            {
                // Fallback: play first available clip if no Idle state is defined.
                var first = profile.animData.animations[0];
                float speed = first.speedModifier > 0f ? first.speedModifier : 1f;
                _portraitAnimator.PlayAnimation(
                    profile.animData.textureArray,
                    first.startFrame,
                    first.frameCount,
                    first.fps * speed);
            }
        }

        /// <summary>Central toggle so all visibility changes go through one path.</summary>
        private void SetPanelActive(bool active)
        {
            if (panelRoot != null)
                panelRoot.SetActive(active);
        }
    }
}
