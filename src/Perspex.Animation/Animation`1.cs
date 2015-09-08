





namespace Perspex.Animation
{
    using System;

    /// <summary>
    /// Tracks the progress of an animation.
    /// </summary>
    /// <typeparam name="T">The type of the value being animated./</typeparam>
    public class Animation<T> : IObservable<T>, IDisposable
    {
        /// <summary>
        /// The animation being tracked.
        /// </summary>
        private IObservable<T> inner;

        /// <summary>
        /// The disposable used to cancel the animation.
        /// </summary>
        private IDisposable subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="Animation{T}"/> class.
        /// </summary>
        /// <param name="inner">The animation observable being tracked.</param>
        /// <param name="subscription">A disposable used to cancel the animation.</param>
        public Animation(IObservable<T> inner, IDisposable subscription)
        {
            this.inner = inner;
            this.subscription = subscription;
        }

        /// <summary>
        /// Cancels the animation.
        /// </summary>
        public void Dispose()
        {
            this.subscription.Dispose();
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications
        /// before the provider has finished sending them.
        /// </returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return this.inner.Subscribe(observer);
        }
    }
}
