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
    /// an observable in two major ways:
    /// 
    /// - Can only have a single subscription
    /// - The subscription can have a tag associated with it, allowing a subscriber to index
    ///   into a list of subscriptions without having to allocate additional objects.
    /// </remarks>
    [Unstable]
    public interface IStyleActivator : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the style is activated.
        /// </summary>
        /// <remarks>
        /// This property should read directly from its inputs and not rely on any subscriptions 
        /// to fire in order to be up-to-date. If a change in active state occurs when reading
        /// this property then any subscribed <see cref="IStyleActivatorSink"/> should not be
        /// notified of the change.
        /// </remarks>
        bool IsActive { get; }

        /// <summary>
        /// Gets a value indicating whether the style is subscribed.
        /// </summary>
        bool IsSubscribed { get; }

        /// <summary>
        /// Subscribes to the activator.
        /// </summary>
        /// <param name="sink">The listener.</param>
        /// <param name="tag">An optional tag.</param>
        /// <remarks>
        /// This method should not call <see cref="IStyleActivatorSink.OnNext(bool, int)"/>.
        /// </remarks>
        void Subscribe(IStyleActivatorSink sink, int tag = 0);

        /// <summary>
        /// Unsubscribes from the activator.
        /// </summary>
        void Unsubscribe(IStyleActivatorSink sink);
    }
}
