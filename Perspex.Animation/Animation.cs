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
        private IObservable<object> inner;

        private IDisposable subscription;

        public Animation(IObservable<object> inner, IDisposable subscription)
        {
            this.inner = inner;
            this.subscription = subscription;
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            return this.inner.Subscribe(observer);
        }
    }

    /// <summary>
    /// Tracks the progress of an animation.
    /// </summary>
    public class Animation<T> : IObservable<T>, IDisposable
    {
        private IObservable<T> inner;

        private IDisposable subscription;

        public Animation(IObservable<T> inner, IDisposable subscription)
        {
            this.inner = inner;
            this.subscription = subscription;
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return this.inner.Subscribe(observer);
        }
    }
}
