using System;

namespace FD.Events
{
    /// <summary>
    /// Event bus pattern - Decouples publishers và subscribers
    /// Thay thế cho callbacks trực tiếp
    /// </summary>
    public interface IGameplayEventBus
    {
        void Publish<TEvent>(TEvent eventData) where TEvent : IGameplayEvent;
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameplayEvent;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameplayEvent;
    }
    
    /// <summary>
    /// Base interface cho tất cả events
    /// </summary>
    public interface IGameplayEvent
    {
        float Timestamp { get; }
    }
}
