using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices; // Required for IsReadOnlyAttribute
using UnityEngine;
using R3;

namespace FD
{
    public class EventBus : IEventBus, IDisposable
    {
        // Maintains a dictionary mapping the Event Type to its corresponding R3 Subject.
        private readonly Dictionary<Type, object> _subjects = new Dictionary<Type, object>();

        // Backwards compatibility mappings for Action subscriptions
        private readonly Dictionary<Delegate, IDisposable> _actionBindings = new Dictionary<Delegate, IDisposable>();

        // Cache to store types that have already been validated (optimization for Editor).
        private static readonly HashSet<Type> _validatedTypes = new HashSet<Type>();

        /// <inheritdoc />
        public Observable<T> Receive<T>() where T : struct
        {
#if UNITY_EDITOR
            ValidateReadonlyStruct<T>();
#endif
            var type = typeof(T);
            if (!_subjects.TryGetValue(type, out var subject))
            {
                subject = new Subject<T>();
                _subjects[type] = subject;
            }

            return ((Subject<T>)subject).AsObservable();
        }

        public void Publish<T>(T eventMessage) where T : struct
        {
#if UNITY_EDITOR
            ValidateReadonlyStruct<T>();
#endif
            var type = typeof(T);
            if (_subjects.TryGetValue(type, out var subject))
            {
                ((Subject<T>)subject).OnNext(eventMessage);
            }
        }

        public void Subscribe<T>(Action<T> listener) where T : struct
        {
            if (listener == null) return;
            if (_actionBindings.ContainsKey(listener)) return;

            var subscription = Receive<T>().Subscribe(listener);
            _actionBindings[listener] = subscription;
        }

        public void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            if (listener == null) return;

            if (_actionBindings.TryGetValue(listener, out var subscription))
            {
                subscription.Dispose();
                _actionBindings.Remove(listener);
            }
        }

        public void Dispose()
        {
            foreach (var binding in _actionBindings.Values)
            {
                binding.Dispose();
            }
            _actionBindings.Clear();

            foreach (var subject in _subjects.Values)
            {
                if (subject is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _subjects.Clear();
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