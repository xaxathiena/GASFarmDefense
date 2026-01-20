using System.Collections.Generic;
using UnityEngine;

namespace _Master.Base.Ability
{
    /// <summary>
    /// Base class for attribute sets. Create your own attribute set by inheriting from this.
    /// </summary>
    public abstract class AttributeSet : ScriptableObject
    {
        protected AbilitySystemComponent ownerASC;
        
        // Dictionary to store all attributes by name
        protected Dictionary<string, GameplayAttribute> attributes = new Dictionary<string, GameplayAttribute>();
        
        /// <summary>
        /// Initialize the attribute set with owner
        /// </summary>
        public virtual void InitAttributeSet(AbilitySystemComponent asc)
        {
            ownerASC = asc;
            OnAttributeSetInitialized();
        }
        
        /// <summary>
        /// Called when attribute set is initialized
        /// </summary>
        protected virtual void OnAttributeSetInitialized()
        {
        }
        
        /// <summary>
        /// Register an attribute to the attribute set using enum
        /// </summary>
        protected void RegisterAttribute<T>(T attributeType, GameplayAttribute attribute) where T : System.Enum
        {
            string name = attributeType.ToString();
            RegisterAttribute(name, attribute);
        }
        
        /// <summary>
        /// Register an attribute to the attribute set using string
        /// </summary>
        protected void RegisterAttribute(string name, GameplayAttribute attribute)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("Cannot register attribute with null or empty name!");
                return;
            }
            
            attributes[name] = attribute;
            
            // Subscribe to value changes for callbacks
            attribute.OnValueChanged += (oldVal, newVal) => PostAttributeChange(attribute, oldVal, newVal);
        }
        
        /// <summary>
        /// Get attribute by enum
        /// </summary>
        public GameplayAttribute GetAttribute<T>(T attributeType) where T : System.Enum
        {
            string name = attributeType.ToString();
            return GetAttribute(name);
        }
        
        /// <summary>
        /// Get attribute by name
        /// </summary>
        public GameplayAttribute GetAttribute(string name)
        {
            if (attributes.TryGetValue(name, out var attribute))
                return attribute;
            
            return null;
        }
        
        /// <summary>
        /// Check if attribute exists using enum
        /// </summary>
        public bool HasAttribute<T>(T attributeType) where T : System.Enum
        {
            string name = attributeType.ToString();
            return HasAttribute(name);
        }
        
        /// <summary>
        /// Check if attribute exists
        /// </summary>
        public bool HasAttribute(string name)
        {
            return attributes.ContainsKey(name);
        }
        
        /// <summary>
        /// Get all attribute names
        /// </summary>
        public IEnumerable<string> GetAttributeNames()
        {
            return attributes.Keys;
        }
        
        /// <summary>
        /// Get all attributes
        /// </summary>
        public Dictionary<string, GameplayAttribute> GetAllAttributes()
        {
            return new Dictionary<string, GameplayAttribute>(attributes);
        }
        
        /// <summary>
        /// Called before an attribute is modified
        /// </summary>
        protected virtual void PreAttributeChange(GameplayAttribute attribute, float delta)
        {
        }
        
        /// <summary>
        /// Called after an attribute is modified
        /// </summary>
        protected virtual void PostAttributeChange(GameplayAttribute attribute, float oldValue, float newValue)
        {
        }
    }
}
