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

    internal class SetterSubject : IObservable<object>
    {
        private Control control;

        private object onValue;

        private object offValue;

        private List<IObserver<object>> observers;

        public SetterSubject(Control control, object onValue, object offValue)
        {
            this.control = control;
            this.onValue = onValue;
            this.offValue = offValue;
            this.observers = new List<IObserver<object>>();
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

    public class Setter
    {
        private object oldValue;

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

        internal SetterSubject CreateSubject(Control control)
        {
            object oldValue = control.ExtractBinding(this.Property) ?? control.GetValue(this.Property);
            return new SetterSubject(control, this.Value, oldValue);
        }
    }
}
