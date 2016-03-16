﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Perspex.Data;
using Perspex.Metadata;
using Perspex.Reactive;

namespace Perspex.Styling
{
    /// <summary>
    /// A setter for a <see cref="Style"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Setter"/> is used to set a <see cref="PerspexProperty"/> value on a
    /// <see cref="PerspexObject"/> depending on a condition.
    /// </remarks>
    public class Setter : ISetter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        public Setter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The property value.</param>
        public Setter(PerspexProperty property, object value)
        {
            Property = property;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the property to set.
        /// </summary>
        public PerspexProperty Property
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [Content]
        [AssignBinding]
        [DependsOn(nameof(Property))]
        public object Value
        {
            get;
            set;
        }

        /// <summary>
        /// Applies the setter to a control.
        /// </summary>
        /// <param name="style">The style that is being applied.</param>
        /// <param name="control">The control.</param>
        /// <param name="activator">An optional activator.</param>
        public IDisposable Apply(IStyle style, IStyleable control, IObservable<bool> activator)
        {
            Contract.Requires<ArgumentNullException>(control != null);

            var description = style?.ToString();

            if (Property == null)
            {
                throw new InvalidOperationException("Setter.Property must be set.");
            }

            var binding = Value as IBinding;

            if (binding == null)
            {
                if (activator == null)
                {
                    return control.Bind(Property, ObservableEx.SingleValue(Value), BindingPriority.Style);
                }
                else
                {
                    var activated = new ActivatedValue(activator, Value, description);
                    return control.Bind(Property, activated, BindingPriority.StyleTrigger);
                }
            }
            else
            {
                var source = binding.Initiate(control, Property);

                if (source != null)
                {
                    var cloned = Clone(source, style, activator);
                    return BindingOperations.Apply(control, Property, cloned, null);
                }
            }

            return Disposable.Empty;
        }

        private InstancedBinding Clone(InstancedBinding sourceInstance, IStyle style, IObservable<bool> activator)
        {
            InstancedBinding cloned;

            if (activator != null)
            {
                var description = style?.ToString();

                if (sourceInstance.Subject != null)
                {
                    var activated = new ActivatedSubject(activator, sourceInstance.Subject, description);
                    cloned = new InstancedBinding(activated, sourceInstance.Mode, BindingPriority.StyleTrigger);
                }
                else if (sourceInstance.Observable != null)
                {
                    var activated = new ActivatedObservable(activator, sourceInstance.Observable, description);
                    cloned = new InstancedBinding(activated, sourceInstance.Mode, BindingPriority.StyleTrigger);
                }
                else
                {
                    var activated = new ActivatedValue(activator, sourceInstance.Value, description);
                    cloned = new InstancedBinding(activated, BindingMode.OneWay, BindingPriority.StyleTrigger);
                }
            }
            else
            {
                if (sourceInstance.Subject != null)
                {
                    cloned = new InstancedBinding(sourceInstance.Subject, sourceInstance.Mode, BindingPriority.Style);
                }
                else if (sourceInstance.Observable != null)
                {
                    cloned = new InstancedBinding(sourceInstance.Observable, sourceInstance.Mode, BindingPriority.Style);
                }
                else
                {
                    cloned = new InstancedBinding(sourceInstance.Value, BindingPriority.Style);
                }
            }

            return cloned;
        }
    }
}
