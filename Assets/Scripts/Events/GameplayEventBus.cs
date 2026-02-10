using System;
using System.Collections.Generic;

namespace FD.Events
{
    /// <summary>
    /// Implementation của event bus
    /// </summary>
    public class GameplayEventBus : IGameplayEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        
        public void Publish<TEvent>(TEvent eventData) where TEvent : IGameplayEvent
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
                return;
            
            var handlers = _subscribers[eventType];
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (i < handlers.Count) // Safety check during iteration
                {
                    ((Action<TEvent>)handlers[i])?.Invoke(eventData);
                }
            }
        }
        
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameplayEvent
        {
            if (handler == null)
                return;
            
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            
            if (!_subscribers[eventType].Contains(handler))
            {
                _subscribers[eventType].Add(handler);
            }
        }
        
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameplayEvent
        {
            if (handler == null)
                return;
            
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
                return;
            
            _subscribers[eventType].Remove(handler);
        }
        
        public void Clear()
        {
            _subscribers.Clear();
        }
    }
}
