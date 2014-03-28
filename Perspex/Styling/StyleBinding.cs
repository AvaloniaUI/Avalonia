// -----------------------------------------------------------------------
// <copyright file="StyleBinding.cs" company="Tricycle">
// Copyright 2013 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Reactive.Subjects;

    /// <summary>
    /// Provides an observable for a style.
    /// </summary>
    /// <remarks>
    /// This class takes an activator and a value. The activator is an observable which produces
    /// a bool. When the activator produces true, this observable will produce <see cref="Value"/>.
    /// When the activator produces false (and before the activator returns a value) it will 
    /// produce <see cref="PerspexProperty.UnsetValue"/>.
    /// </remarks>
    internal class StyleBinding : IObservable<object>, IObservableDescription
    {
        /// <summary>
        /// The subject that provides the observable implementation.
        /// </summary>
        private BehaviorSubject<object> subject = new BehaviorSubject<object>(PerspexProperty.UnsetValue);

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
            this.ActivatedValue = activatedValue;
            this.Description = description;

            activator.Subscribe(
                active => this.subject.OnNext(active ? this.ActivatedValue : PerspexProperty.UnsetValue),
                error => this.subject.OnError(error),
                () => this.subject.OnCompleted());
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
        public IDisposable Subscribe(IObserver<object> observer)
        {
            Contract.Requires<NullReferenceException>(observer != null);
            return this.subject.Subscribe(observer);
        }
    }
}
