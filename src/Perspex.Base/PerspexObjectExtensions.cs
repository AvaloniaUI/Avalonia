





namespace Perspex
{
    using System;
    using System.Reactive.Linq;

    /// <summary>
    /// Provides extension methods for <see cref="PerspexObject"/> and related classes.
    /// </summary>
    public static class PerspexObjectExtensions
    {
        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="action">
        /// The method to call. The parameters are the sender and the event args.
        /// </param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget>(
            this IObservable<PerspexPropertyChangedEventArgs> observable,
            Action<TTarget, PerspexPropertyChangedEventArgs> action)
            where TTarget : PerspexObject
        {
            return observable.Subscribe(e => action((TTarget)e.Sender, e));
        }

        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="handler">Given a TTarget, returns the handler.</param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget>(
            this IObservable<PerspexPropertyChangedEventArgs> observable,
            Func<TTarget, Action<PerspexPropertyChangedEventArgs>> handler)
            where TTarget : class
        {
            return observable.Subscribe(e => SubscribeAdapter(e, handler));
        }

        /// <summary>
        /// Observer method for <see cref="AddClassHandler{TTarget}(IObservable{PerspexPropertyChangedEventArgs},
        /// Func{TTarget, Action{PerspexPropertyChangedEventArgs}})"/>.
        /// </summary>
        /// <typeparam name="TTarget">The sender type to accept.</typeparam>
        /// <param name="e">The event args.</param>
        /// <param name="handler">Given a TTarget, returns the handler.</param>
        private static void SubscribeAdapter<TTarget>(
            PerspexPropertyChangedEventArgs e,
            Func<TTarget, Action<PerspexPropertyChangedEventArgs>> handler)
            where TTarget : class
        {
            var target = e.Sender as TTarget;

            if (target != null)
            {
                handler(target)(e);
            }
        }
    }
}
