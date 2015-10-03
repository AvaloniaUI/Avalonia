// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;

namespace Perspex
{
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
        /// Updates the target when the application starts or when the data context changes.
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
        public string Description => string.Format("{0}.{1}", Source?.GetType().Name, Property.Name);

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
            Mode = mode;
            return this;
        }

        /// <summary>
        /// Modifies the binding priority.
        /// </summary>
        /// <param name="priority">The binding priority.</param>
        /// <returns>The object that the method was called on.</returns>
        public BindingDescriptor WithPriority(BindingPriority priority)
        {
            Priority = priority;
            return this;
        }

        /// <inheritdoc/>
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            return Source.GetObservable(Property).Subscribe(observer);
        }
    }
}
