using System;
using GAS;
using UnityEngine;

namespace FD.Views
{
    /// <summary>
    /// View layer cho enemy - Chỉ là "bù nhìn"!
    /// ✅ Không có game logic
    /// ✅ Không có static calls
    /// ✅ Chỉ expose Unity properties và lifecycle events
    /// </summary>
    public class EnemyView : MonoBehaviour, IAbilitySystemComponent
    {
        public AbilitySystemComponent ownerASC;
        // Unity compnent references (optional)
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer meshRenderer;
        
        // Public properties để đọc từ controller
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public int Layer => gameObject.layer;
        public bool IsActive => gameObject.activeInHierarchy;
        public Vector3 Position => transform.position;

        public AbilitySystemComponent AbilitySystemComponent => ownerASC;

        // Lifecycle events - Controller sẽ subscribe
        public event Action<EnemyView> OnSpawned;
        public event Action<EnemyView> OnDespawned;
        public event Action<EnemyView> OnDestroyed;
        
        // Unity callbacks - Chỉ raise events
        private void OnEnable()
        {
            OnSpawned?.Invoke(this);
        }
        
        private void OnDisable()
        {
            OnDespawned?.Invoke(this);
        }
        
        private void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }
        
        // View update methods - Called by controller
        public void UpdatePosition(Vector3 newPosition)
        {
            transform.position = newPosition;
        }
        
        public void UpdateRotation(Quaternion newRotation)
        {
            transform.rotation = newRotation;
        }
        
        public void LookAt(Vector3 target)
        {
            transform.LookAt(target);
        }
        
        // Animation control (if animator exists)
        public void PlayAnimation(string animationName)
        {
            if (animator != null)
                animator.Play(animationName);
        }
        
        public void SetAnimationFloat(string paramName, float value)
        {
            if (animator != null)
                animator.SetFloat(paramName, value);
        }
        
        public void SetAnimationBool(string paramName, bool value)
        {
            if (animator != null)
                animator.SetBool(paramName, value);
        }
        
        // Visual effects
        public void SetColor(Color color)
        {
            if (meshRenderer != null)
            {
                var propBlock = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", color);
                meshRenderer.SetPropertyBlock(propBlock);
            }
        }
        
        // Destroy helper
        public void DestroyView()
        {
            Destroy(gameObject);
        }
    }
}
