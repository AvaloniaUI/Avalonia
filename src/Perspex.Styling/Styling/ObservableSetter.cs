// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Styling
{
    /// <summary>
    /// A setter for a <see cref="Style"/> whose source is an observable.
    /// </summary>
    /// <remarks>
    /// A <see cref="Setter"/> is used to set a <see cref="PerspexProperty"/> value on a
    /// <see cref="PerspexObject"/> depending on a condition.
    /// </remarks>
    public class ObservableSetter : ISetter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSetter"/> class.
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="source">An observable which produces the value for the property.</param>
        public ObservableSetter(PerspexProperty property, IObservable<object> source)
        {
            this.Property = property;
            this.Source = source;
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
        /// Gets or sets an observable which produces the value for the property.
        /// </summary>
        public IObservable<object> Source
        {
            get;
            set;
        }

        /// <summary>
        /// Applies the setter to the control.
        /// </summary>
        /// <param name="style">The style that is being applied.</param>
        /// <param name="control">The control.</param>
        /// <param name="activator">An optional activator.</param>
        public void Apply(IStyle style, IStyleable control, IObservable<bool> activator)
        {
            if (activator == null)
            {
                control.Bind(this.Property, this.Source, BindingPriority.Style);
            }
            else
            {
                var binding = new StyleBinding(activator, this.Source, style.ToString());
                control.Bind(this.Property, binding, BindingPriority.StyleTrigger);
            }
        }
    }
}
