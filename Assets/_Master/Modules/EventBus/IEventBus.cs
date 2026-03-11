using System;

namespace FD
{
    public interface IEventBus
    {
        /// <summary>
        /// Registers to receive an event stream of type T in a Reactive way using R3.
        /// </summary>
        R3.Observable<T> Receive<T>() where T : struct;

        /// <summary>
        /// Publish an event to all subscribers.
        /// The event must be a struct (preferably readonly) to avoid GC allocation.
        /// </summary>
        void Publish<T>(T eventMessage) where T : struct;

        /// <summary>
        /// Subscribe to a specific event type via callbacks.
        /// Return to R3.Receive() when possible for better memory control.
        /// </summary>
        void Subscribe<T>(Action<T> listener) where T : struct;

        /// <summary>
        /// Unsubscribe from a specific event type.
        /// </summary>
        void Unsubscribe<T>(Action<T> listener) where T : struct;
    }
}