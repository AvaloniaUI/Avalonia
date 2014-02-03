// -----------------------------------------------------------------------
// <copyright file="PriorityValue.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reactive.Disposables;

    internal class PriorityValue : IObservable<Tuple<object, object>>
    {
        private object localValue = PerspexProperty.UnsetValue;

        private IDisposable localBinding;

        private object lastValue = PerspexProperty.UnsetValue;

        private List<StyleEntry> styles = new List<StyleEntry>();

        private List<IObserver<Tuple<object, object>>> observers = 
            new List<IObserver<Tuple<object, object>>>();

        public object LocalValue
        {
            get
            {
                return this.localValue;
            }

            set
            {
                if (!object.Equals(this.localValue, value))
                {
                    this.localValue = value;
                    this.Push();
                }
            }
        }

        public void ClearLocalBinding()
        {
            if (this.localBinding != null)
            {
                this.localBinding.Dispose();
            }
        }

        public void SetLocalValue(object value)
        {
            if (this.localBinding != null)
            {
                this.localBinding.Dispose();
            }

            this.LocalValue = value;
        }

        public void SetLocalBinding(IObservable<object> binding)
        {
            if (this.localBinding != null)
            {
                this.localBinding.Dispose();
            }

            this.localBinding = binding.Subscribe(value => this.LocalValue = value);
        }

        public void AddStyle(object value)
        {
            StyleEntry entry = new StyleEntry(value);

            this.styles.Add(entry);

            if (this.localValue == PerspexProperty.UnsetValue)
            {
                this.Push();
            }
        }

        public void AddStyle(IObservable<bool> activator, object value)
        {
            Contract.Requires<NullReferenceException>(activator != null);

            StyleEntry entry = new StyleEntry(activator, value, this.Push, e => this.styles.Remove(e));

            this.styles.Add(entry);

            if (this.localValue == PerspexProperty.UnsetValue)
            {
                this.Push();
            }
        }

        public object GetEffectiveValue()
        {
            if (this.localValue != PerspexProperty.UnsetValue)
            {
                return this.localValue;
            }
            else
            {
                foreach (StyleEntry style in Enumerable.Reverse(this.styles))
                {
                    if (style.Active)
                    {
                        return style.Value;
                    }
                }
            }

            return PerspexProperty.UnsetValue;
        }

        public IDisposable Subscribe(IObserver<Tuple<object, object>> observer)
        {
            Contract.Requires<NullReferenceException>(observer != null);

            this.observers.Add(observer);

            return Disposable.Create(() => this.observers.Remove(observer));
        }

        private void Push()
        {
            object value = this.GetEffectiveValue();

            if (!object.Equals(this.lastValue, value))
            {
                foreach (IObserver<Tuple<object, object>> observer in this.observers)
                {
                    observer.OnNext(Tuple.Create(this.lastValue, value));
                }

                this.lastValue = value;
            }
        }

        private class StyleEntry
        {
            private IObservable<bool> activator;

            public StyleEntry(object value)
            {
                this.Active = true;
                this.Value = value;
            }

            public StyleEntry(
                IObservable<bool> activator, 
                object value, 
                Action activeChanged,
                Action<StyleEntry> completed)
            {
                Contract.Requires<NullReferenceException>(activator != null);
                Contract.Requires<NullReferenceException>(activeChanged != null);

                this.activator = activator;
                this.Value = value;

                this.activator.Subscribe(x => this.Active = x, () => completed(this));
            }

            public bool Active 
            { 
                get; 
                private set; 
            }

            public object Value
            {
                get;
                private set;
            }
        }
    }
}
