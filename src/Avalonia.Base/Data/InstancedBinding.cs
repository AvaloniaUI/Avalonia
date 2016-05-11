// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Avalonia.Data
{
    /// <summary>
    /// Holds the result of calling <see cref="IBinding.Initiate"/>.
    /// </summary>
    /// <remarks>
    /// Whereas an <see cref="IBinding"/> holds a description of a binding such as "Bind to the X
    /// property on a control's DataContext"; this class represents a binding that has been 
    /// *instanced* by calling <see cref="IBinding.Initiate(IAvaloniaObject, AvaloniaProperty, object)"/>
    /// on a target object.
    /// 
    /// When a binding is initiated, it can return one of 3 possible sources for the binding:
    /// - An <see cref="ISubject{Object}"/> which can be used for any type of binding.
    /// - An <see cref="IObservable{Object}"/> which can be used for all types of bindings except
    ///  <see cref="BindingMode.OneWayToSource"/> and <see cref="BindingMode.TwoWay"/>.
    /// - A plain object, which can only represent a <see cref="BindingMode.OneTime"/> binding.
    /// </remarks>
    public class InstancedBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstancedBinding"/> class.
        /// </summary>
        /// <param name="value">
        /// The value used for the <see cref="BindingMode.OneTime"/> binding.
        /// </param>
        /// <param name="priority">The binding priority.</param>
        public InstancedBinding(object value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Mode = BindingMode.OneTime;
            Priority = priority;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstancedBinding"/> class.
        /// </summary>
        /// <param name="observable">The observable for a one-way binding.</param>
        /// <param name="mode">The binding mode.</param>
        /// <param name="priority">The binding priority.</param>
        public InstancedBinding(
            IObservable<object> observable, 
            BindingMode mode = BindingMode.OneWay,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(observable != null);

            if (mode == BindingMode.OneWayToSource || mode == BindingMode.TwoWay)
            {
                throw new ArgumentException(
                    "Invalid BindingResult mode: OneWayToSource and TwoWay bindings" +
                    "require a Subject.");
            }

            Mode = mode;
            Priority = priority;
            Observable = observable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstancedBinding"/> class.
        /// </summary>
        /// <param name="subject">The subject for a two-way binding.</param>
        /// <param name="mode">The binding mode.</param>
        /// <param name="priority">The binding priority.</param>
        public InstancedBinding(
            ISubject<object> subject,
            BindingMode mode = BindingMode.OneWay,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(subject != null);

            Mode = mode;
            Priority = priority;
            Subject = subject;
        }

        /// <summary>
        /// Gets the binding mode with which the binding was initiated.
        /// </summary>
        public BindingMode Mode { get; }

        /// <summary>
        /// Gets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; }

        /// <summary>
        /// Gets the value used for a <see cref="BindingMode.OneTime"/> binding.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets the observable for a one-way binding.
        /// </summary>
        public IObservable<object> Observable { get; }

        /// <summary>
        /// Gets the subject for a two-way binding.
        /// </summary>
        public ISubject<object> Subject { get; }
    }
}
