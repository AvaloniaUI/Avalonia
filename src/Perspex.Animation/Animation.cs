// -----------------------------------------------------------------------
// <copyright file="Animation.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;

    /// <summary>
    /// Tracks the progress of an animation.
    /// </summary>
    public class Animation : IObservable<object>, IDisposable
    {
        /// <summary>
        /// The animation being tracked.
        /// </summary>
        private IObservable<object> inner;

        /// <summary>
        /// The disposable used to cancel the animation.
        /// </summary>
        private IDisposable subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="Animation"/> class.
        /// </summary>
        /// <param name="inner">The animation observable being tracked.</param>
        /// <param name="subscription">A disposable used to cancel the animation.</param>
        public Animation(IObservable<object> inner, IDisposable subscription)
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
        public IDisposable Subscribe(IObserver<object> observer)
        {
            return this.inner.Subscribe(observer);
        }
    }
}
