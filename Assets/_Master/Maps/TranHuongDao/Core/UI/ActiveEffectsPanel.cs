using System.Collections.Generic;
using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Manages a pool of <see cref="ActiveEffectSlotView"/> instances and keeps them
    /// in sync with the active gameplay effects on the currently selected unit.
    /// </summary>
    public class ActiveEffectsPanel : MonoBehaviour
    {
        [SerializeField] private Transform slotContainer;
        [SerializeField] private ActiveEffectSlotView slotPrefab;
        [Tooltip("Maximum number of effect slots shown at once.")]
        [SerializeField] private int maxSlots = 8;

        // ── Slot pool ─────────────────────────────────────────────────────────────

        private readonly List<ActiveEffectSlotView> _pool = new List<ActiveEffectSlotView>();

        // Currently active (visible) effects — kept for Tick() to avoid re-querying each frame.
        private readonly List<ActiveGameplayEffect> _activeEffects = new List<ActiveGameplayEffect>();
        private AbilitySystemComponent _currentASC;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Pre-warm pool
            for (int i = 0; i < maxSlots; i++)
            {
                var slot = Instantiate(slotPrefab, slotContainer);
                slot.Hide();
                _pool.Add(slot);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Populate slots from the ASC's active effects.
        /// Call on selection and every frame (cheap — GAS list is tiny).
        /// Skips Instant effects (they fire-and-forget, nothing to display).
        /// </summary>
        public void Refresh(AbilitySystemComponent asc)
        {
            _currentASC = asc;

            _activeEffects.Clear();

            if (asc != null)
            {
                foreach (var effect in asc.GetActiveGameplayEffects())
                {
                    // Instant effects have no persistent state to show.
                    if (effect.Effect.durationType == EGameplayEffectDurationType.Instant)
                        continue;

                    _activeEffects.Add(effect);

                    if (_activeEffects.Count >= maxSlots)
                        break;
                }
            }

            // Bind visible slots
            for (int i = 0; i < _pool.Count; i++)
            {
                if (i < _activeEffects.Count)
                    _pool[i].Bind(_activeEffects[i]);
                else
                    _pool[i].Hide();
            }
        }

        /// <summary>Update duration bars on all visible slots. Call every frame.</summary>
        public void Tick()
        {
            for (int i = 0; i < _activeEffects.Count && i < _pool.Count; i++)
                _pool[i].Tick();
        }

        /// <summary>Hide all slots and clear cached state.</summary>
        public void Clear()
        {
            _currentASC = null;
            _activeEffects.Clear();
            foreach (var slot in _pool)
                slot.Hide();
        }
    }
}
