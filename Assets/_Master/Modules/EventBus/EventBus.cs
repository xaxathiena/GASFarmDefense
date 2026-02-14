using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices; // Required for IsReadOnlyAttribute
using UnityEngine;

namespace FD
{
    public class EventBus : IEventBus
    {
        // Dictionary to store subscribers.
        // Key: Type of the Event (struct).
        // Value: The delegate (Action<T>) containing all listeners.
        private readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        // Cache to store types that have already been validated (optimization for Editor).
        private static readonly HashSet<Type> _validatedTypes = new HashSet<Type>();

        public void Publish<T>(T eventMessage) where T : struct
        {
#if UNITY_EDITOR
            // Validate that the struct is readonly (Editor only).
            ValidateReadonlyStruct<T>();
#endif

            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var del))
            {
                // Safe cast and invoke.
                // Since T is a struct, this does not cause boxing/unboxing allocation.
                (del as Action<T>)?.Invoke(eventMessage);
            }
        }

        public void Subscribe<T>(Action<T> listener) where T : struct
        {
#if UNITY_EDITOR
            ValidateReadonlyStruct<T>();
#endif
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = null;
            }
            // Combine delegates (Multicast).
            _subscribers[type] = Delegate.Combine(_subscribers[type], listener);
        }

        public void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
            {
                var current = _subscribers[type];
                _subscribers[type] = Delegate.Remove(current, listener);

                // If no listeners remain, remove the key to keep the dictionary clean.
                if (_subscribers[type] == null)
                {
                    _subscribers.Remove(type);
                }
            }
        }

        // --- VALIDATION LOGIC (EDITOR ONLY) ---
        // This ensures developers follow the architecture rule: All events must be readonly structs.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ValidateReadonlyStruct<T>()
        {
            var type = typeof(T);
            
            // Skip if already checked.
            if (_validatedTypes.Contains(type)) return;

            // Check if the struct has the [IsReadOnly] attribute.
            // This attribute is automatically added by the compiler for 'readonly struct'.
            bool isReadOnly = type.GetCustomAttribute<IsReadOnlyAttribute>() != null;

            if (!isReadOnly)
            {
                Debug.LogError($"[EventBus VIOLATION] Event '<color=red>{type.Name}</color>' is NOT a 'readonly struct'!\n" +
                               "Rule: All events must be immutable to ensure thread safety and data integrity.\n" +
                               $"Fix: Change definition to 'public readonly struct {type.Name} {{ ... }}'");
            }

            _validatedTypes.Add(type);
        }
    }
}