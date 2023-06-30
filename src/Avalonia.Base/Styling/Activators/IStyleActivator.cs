using System;
using Avalonia.Metadata;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// Defines a style activator.
    /// </summary>
    /// <remarks>
    /// A style activator is very similar to an `IObservable{bool}` but is optimized for the
    /// particular use-case of activating a style according to a selector. It differs from
    /// an observable in three major ways:
    /// 
    /// - Can only have a single subscription
    /// - The activation state can be re-evaluated at any time by calling <see cref="GetIsActive"/>
    /// - No error or completion messages
    /// </remarks>
    internal interface IStyleActivator : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the style is subscribed.
        /// </summary>
        bool IsSubscribed { get; }

        /// <summary>
        /// Gets the current activation state.
        /// </summary>
        /// <remarks>
        /// This method should read directly from its inputs and not rely on any subscriptions 
        /// to fire in order to be up-to-date. If a change in active state occurs when reading
        /// this method then any subscribed <see cref="IStyleActivatorSink"/> should not be
        /// notified of the change.
        /// </remarks>
        bool GetIsActive();

        /// <summary>
        /// Subscribes to the activator.
        /// </summary>
        /// <param name="sink">The listener.</param>
        /// <remarks>
        /// This method should not call <see cref="IStyleActivatorSink.OnNext(bool)"/>.
        /// </remarks>
        void Subscribe(IStyleActivatorSink sink);

        /// <summary>
        /// Unsubscribes from the activator.
        /// </summary>
        void Unsubscribe(IStyleActivatorSink sink);
    }
}
