// -----------------------------------------------------------------------
// <copyright file="StyleBinding.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Reactive;

    /// <summary>
    /// Provides an observable for a style.
    /// </summary>
    /// <remarks>
    /// This class takes an activator and a value. The activator is an observable which produces
    /// a bool. When the activator produces true, this observable will produce
    /// <see cref="ActivatedValue"/>. When the activator produces false it will produce
    /// <see cref="PerspexProperty.UnsetValue"/>.
    /// </remarks>
    internal class StyleBinding : ObservableBase<object>, IDescription
    {
        /// <summary>
        /// The activator.
        /// </summary>
        private IObservable<bool> activator;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBinding"/> class.
        /// </summary>
        /// <param name="activator">The activator.</param>
        /// <param name="activatedValue">The activated value.</param>
        /// <param name="description">The binding description.</param>
        public StyleBinding(
            IObservable<bool> activator,
            object activatedValue,
            string description)
        {
            this.activator = activator;
            this.ActivatedValue = activatedValue;
            this.Description = description;
        }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the activated value.
        /// </summary>
        public object ActivatedValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <returns>IDisposable object used to unsubscribe from the observable sequence.</returns>
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            Contract.Requires<NullReferenceException>(observer != null);
            return this.activator.Subscribe(
                active => observer.OnNext(active ? this.ActivatedValue : PerspexProperty.UnsetValue),
                observer.OnError,
                observer.OnCompleted);
        }
    }
}
