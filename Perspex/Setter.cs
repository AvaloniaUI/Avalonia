// -----------------------------------------------------------------------
// <copyright file="Setter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Reactive.Disposables;
    using Perspex.Controls;

    public class Setter
    {
        private object oldValue;

        public Setter()
        {
        }

        public Setter(PerspexProperty property, object value)
        {
            this.Property = property;
            this.Value = value;
        }

        public PerspexProperty Property
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }

        internal Subject CreateSubject(Control control, string description)
        {
            object oldValue = control.GetValue(this.Property);
            return new Subject(control, this.Value, oldValue, description);
        }

        internal class Subject : IObservable<object>, IBindingDescription
        {
            private Control control;

            private object onValue;

            private object offValue;

            private List<IObserver<object>> observers;

            public Subject(Control control, object onValue, object offValue, string description)
            {
                this.control = control;
                this.onValue = onValue;
                this.offValue = offValue;
                this.observers = new List<IObserver<object>>();
                this.Description = description;
            }

            public string Description
            {
                get;
                private set;
            }

            public IDisposable Subscribe(IObserver<object> observer)
            {
                observers.Add(observer);
                return Disposable.Create(() => this.observers.Remove(observer));
            }

            public void Push(bool on)
            {
                foreach (IObserver<object> o in this.observers)
                {
                    o.OnNext(on ? this.onValue : this.offValue);
                }
            }
        }
    }
}
