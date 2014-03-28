// -----------------------------------------------------------------------
// <copyright file="ActivatedBinding.cs" company="Tricycle">
// Copyright 2013 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading.Tasks;

    internal class StyleBinding : IObservable<object>, IObservableDescription
    {
        private List<IObserver<object>> observers = new List<IObserver<object>>();

        public StyleBinding(
            StyleActivator activator,
            object activatedValue,
            string description)
        {
            this.Activator = activator;
            this.ActivatedValue = activatedValue;
            this.Description = description;

            this.Activator.Subscribe(
                active => this.OnNext(active ? this.ActivatedValue : PerspexProperty.UnsetValue),
                error => this.OnError(error),
                () => this.OnCompleted());
        }

        public StyleActivator Activator
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public object ActivatedValue
        {
            get;
            private set;
        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            Contract.Requires<NullReferenceException>(observer != null);
            this.observers.Add(observer);
            return Disposable.Create(() => this.observers.Remove(observer));
        }

        private void OnCompleted()
        {
            foreach (var observer in this.observers)
            {
                observer.OnCompleted();
            }
        }

        private void OnError(Exception error)
        {
            foreach (var observer in this.observers)
            {
                observer.OnError(error);
            }
        }

        private void OnNext(object value)
        {
            foreach (var observer in this.observers)
            {
                observer.OnNext(value);
            }
        }
    }
}
