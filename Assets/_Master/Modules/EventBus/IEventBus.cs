using System;

namespace FD
{
    public interface IEventBus
    {
        /// <summary>
        /// Publish an event to all subscribers.
        /// The event must be a struct (preferably readonly) to avoid GC allocation.
        /// </summary>
        void Publish<T>(T eventMessage) where T : struct;

        /// <summary>
        /// Subscribe to a specific event type.
        /// </summary>
        void Subscribe<T>(Action<T> listener) where T : struct;

        /// <summary>
        /// Unsubscribe from a specific event type.
        /// Important: Always unsubscribe in Dispose() or OnDestroy() to prevent memory leaks.
        /// </summary>
        void Unsubscribe<T>(Action<T> listener) where T : struct;
    }
}