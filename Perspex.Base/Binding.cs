// -----------------------------------------------------------------------
// <copyright file="Binding.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Reactive;

    public enum BindingMode
    {
        Default,
        OneWay,
        TwoWay,
        OneTime,
        OneWayToSource,
    }

    public class Binding : ObservableBase<object>, IDescription
    {
        public BindingMode Mode
        {
            get;
            set;
        }

        public BindingPriority Priority
        {
            get;
            set;
        }

        public PerspexProperty Property
        {
            get;
            set;
        }

        public PerspexObject Source
        {
            get;
            set;
        }

        public string Description => string.Format("{0}.{1}", this.Source.GetType().Name, this.Property.Name);

        public static Binding operator !(Binding binding)
        {
            return binding.WithMode(BindingMode.TwoWay);
        }

        public static Binding operator ~(Binding binding)
        {
            return binding.WithMode(BindingMode.TwoWay);
        }

        public Binding WithMode(BindingMode mode)
        {
            this.Mode = mode;
            return this;
        }

        public Binding WithPriority(BindingPriority priority)
        {
            this.Priority = priority;
            return this;
        }

        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            return this.Source.GetObservable(this.Property).Subscribe(observer);
        }
    }
}
