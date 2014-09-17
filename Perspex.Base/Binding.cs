// -----------------------------------------------------------------------
// <copyright file="Binding.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    public enum BindingMode
    {
        Default,
        OneWay,
        TwoWay,
        OneTime,
        OneWayToSource,
    }

    public struct Binding
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
    }
}
