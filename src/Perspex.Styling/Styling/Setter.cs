// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
        public void Apply(IStyle style, IStyleable control, IObservable<bool> activator)
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
                    control.SetValue(Property, Value, BindingPriority.Style);
                }
                else
                {
                    var activated = new ActivatedValue(activator, Value, description);
                    var instanced = new InstancedBinding(
                        activated,
                        BindingMode.OneWay,
                        BindingPriority.StyleTrigger);
                    BindingOperations.Apply(control, Property, instanced, null);
                }
            }
            else
            {
                if (activator == null)
                {
                    control.Bind(Property, binding);
                }
                else
                {
                    var sourceInstance = binding.Initiate(control, Property);
                    InstancedBinding activatedInstance;

                    if (sourceInstance.Subject != null)
                    {
                        var activated = new ActivatedSubject(activator, sourceInstance.Subject, description);
                        activatedInstance = new InstancedBinding(activated, sourceInstance.Mode, sourceInstance.Priority);
                    }
                    else if (sourceInstance.Observable != null)
                    {
                        var activated = new ActivatedObservable(activator, sourceInstance.Observable, description);
                        activatedInstance = new InstancedBinding(activated, sourceInstance.Mode, sourceInstance.Priority);
                    }
                    else
                    {
                        var activated = new ActivatedValue(activator, sourceInstance.Value, description);
                        activatedInstance = new InstancedBinding(activated, sourceInstance.Mode, sourceInstance.Priority);
                    }

                    BindingOperations.Apply(control, Property, activatedInstance, null);
                }
            }
        }
    }
}
