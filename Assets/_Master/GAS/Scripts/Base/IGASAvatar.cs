using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Interface for providing spatial data to the Gameplay Ability System.
    /// This decouples GAS from Unity's Transform/GameObject system, allowing it 
    /// to work with custom rendering systems or pure data structures.
    /// </summary>
    public interface IGASAvatar
    {
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        Vector3 Scale { get; }
        
        /// <summary>
        /// Check if the avatar is still valid/alive.
        /// </summary>
        bool IsValid { get; }
    }

    /// <summary>
    /// Default implementation of IGASAvatar that wraps a standard Unity Transform.
    /// </summary>
    public class TransformAvatar : IGASAvatar
    {
        private readonly Transform _transform;

        public TransformAvatar(Transform transform)
        {
            _transform = transform;
        }

        public Vector3 Position => _transform != null ? _transform.position : Vector3.zero;
        public Quaternion Rotation => _transform != null ? _transform.rotation : Quaternion.identity;
        public Vector3 Scale => _transform != null ? _transform.localScale : Vector3.one;
        public bool IsValid => _transform != null;
    }
}
