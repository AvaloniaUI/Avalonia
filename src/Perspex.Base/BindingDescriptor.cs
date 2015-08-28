// -----------------------------------------------------------------------
// <copyright file="BindingDescriptor.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Reactive;

    /// <summary>
    /// Defines possible binding modes.
    /// </summary>
    public enum BindingMode
    {
        /// <summary>
        /// Uses the default binding mode specified for the property.
        /// </summary>
        Default,

        /// <summary>
        /// Binds one way from source to target.
        /// </summary>
        OneWay,

        /// <summary>
        /// Binds two-way with the initial value coming from the target.
        /// </summary>
        TwoWay,

        /// <summary>
        /// Copies the target to the source one time and then disposes of the binding.
        /// </summary>
        OneTime,

        /// <summary>
        /// Binds one way from target to source.
        /// </summary>
        OneWayToSource,
    }

    /// <summary>
    /// Holds a description of a binding, usually for <see cref="PerspexObject"/>'s [] operator.
    /// </summary>
    public class BindingDescriptor : ObservableBase<object>, IDescription
    {
        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source property.
        /// </summary>
        public PerspexProperty Property
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source object.
        /// </summary>
        public PerspexObject Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description => string.Format("{0}.{1}", this.Source?.GetType().Name, this.Property.Name);

        /// <summary>
        /// Makes a two-way binding.
        /// </summary>
        /// <param name="binding">The current binding.</param>
        /// <returns>A two-way binding.</returns>
        public static BindingDescriptor operator !(BindingDescriptor binding)
        {
            return binding.WithMode(BindingMode.TwoWay);
        }

        /// <summary>
        /// Makes a two-way binding.
        /// </summary>
        /// <param name="binding">The current binding.</param>
        /// <returns>A two-way binding.</returns>
        public static BindingDescriptor operator ~(BindingDescriptor binding)
        {
            return binding.WithMode(BindingMode.TwoWay);
        }

        /// <summary>
        /// Modifies the binding mode.
        /// </summary>
        /// <param name="mode">The binding mode.</param>
        /// <returns>The object that the method was called on.</returns>
        public BindingDescriptor WithMode(BindingMode mode)
        {
            this.Mode = mode;
            return this;
        }

        /// <summary>
        /// Modifies the binding priority.
        /// </summary>
        /// <param name="priority">The binding priority.</param>
        /// <returns>The object that the method was called on.</returns>
        public BindingDescriptor WithPriority(BindingPriority priority)
        {
            this.Priority = priority;
            return this;
        }

        /// <inheritdoc/>
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            return this.Source.GetObservable(this.Property).Subscribe(observer);
        }
    }
}
