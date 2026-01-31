using System.Collections.Generic;
using GAS;
using FD.Ability;
using FD.Character;
using UnityEngine;

namespace FD.TrainingArea
{
    /// <summary>
    /// Training player character with ability selection and testing capabilities
    /// </summary>
    public class TrainingPlayer : BaseCharacter
    {
        [Header("Ability Testing")]
        [SerializeField] private List<GameplayAbility> availableAbilities = new List<GameplayAbility>();
        [SerializeField] private int selectedAbilityIndex = 0;
        
        [Header("Target Settings")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float autoTargetRange = 10f;
        [SerializeField] private LayerMask targetLayer;

        public GameplayAbility SelectedAbility => selectedAbilityIndex >= 0 && selectedAbilityIndex < availableAbilities.Count 
            ? availableAbilities[selectedAbilityIndex] 
            : null;

        protected override void Start()
        {
            base.Start();
            InitializeAbilities();
        }

        private void InitializeAbilities()
        {
            // Grant all available abilities to the ability system
            foreach (var ability in availableAbilities)
            {
                if (ability != null)
                {
                    abilitySystemComponent.GiveAbility(ability);
                }
            }
        }

        /// <summary>
        /// Activate the currently selected ability
        /// </summary>
        public void ActivateSelectedAbility()
        {
            if (SelectedAbility == null)
            {
                Debug.LogWarning("No ability selected!");
                return;
            }

            // Auto-find target if not set
            if (targetTransform == null)
            {
                FindNearestTarget();
            }

            // Try to activate the ability
            bool success = abilitySystemComponent.TryActivateAbility(SelectedAbility);
            
            if (success)
            {
                Debug.Log($"Activated ability: {SelectedAbility.name}");
            }
            else
            {
                Debug.LogWarning($"Failed to activate ability: {SelectedAbility.name}");
            }
        }

        /// <summary>
        /// Get targets for abilities (used by ability system)
        /// </summary>
        public override List<Transform> GetTargets()
        {
            var targets = new List<Transform>();
            if (targetTransform != null)
            {
                targets.Add(targetTransform);
            }
            return targets;
        }

        /// <summary>
        /// Select an ability by index
        /// </summary>
        public void SelectAbility(int index)
        {
            if (index >= 0 && index < availableAbilities.Count)
            {
                selectedAbilityIndex = index;
                Debug.Log($"Selected ability: {availableAbilities[index].name}");
            }
        }

        /// <summary>
        /// Add a new ability to the available list
        /// </summary>
        public void AddAbility(GameplayAbility ability)
        {
            if (ability != null && !availableAbilities.Contains(ability))
            {
                availableAbilities.Add(ability);
                abilitySystemComponent.GiveAbility(ability);
            }
        }

        /// <summary>
        /// Remove an ability from the available list
        /// </summary>
        public void RemoveAbility(GameplayAbility ability)
        {
            if (ability != null && availableAbilities.Contains(ability))
            {
                availableAbilities.Remove(ability);
                // Note: ASC doesn't have RemoveAbility, so we just remove from our list
            }
        }

        /// <summary>
        /// Set the target for abilities
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
        }

        /// <summary>
        /// Find nearest target within range
        /// </summary>
        public void FindNearestTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, autoTargetRange, targetLayer);
            float nearestDistance = float.MaxValue;
            Transform nearestTarget = null;

            foreach (var collider in colliders)
            {
                if (collider.transform != transform)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = collider.transform;
                    }
                }
            }

            targetTransform = nearestTarget;
            if (nearestTarget != null)
            {
                Debug.Log($"Auto-targeted: {nearestTarget.name}");
            }
        }

        /// <summary>
        /// Reset player stats to full
        /// </summary>
        public void ResetStats()
        {
            if (attributeSet != null)
            {
                attributeSet.Health.SetCurrentValue(attributeSet.MaxHealth.CurrentValue);
                attributeSet.Mana.SetCurrentValue(attributeSet.MaxMana.CurrentValue);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw auto-target range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, autoTargetRange);

            // Draw line to current target
            if (targetTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, targetTransform.position);
            }
        }
    }
}
