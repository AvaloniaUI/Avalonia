// -----------------------------------------------------------------------
// <copyright file="Setter.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;

    /// <summary>
    /// A setter for a <see cref="Style"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Setter"/> is used to set a <see cref="PerspexProperty"/> value on a
    /// <see cref="PerspexObject"/> depending on a condition.
    /// </remarks>
    public class Setter
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
            this.Property = property;
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="source">An observable which produces the value for the property.</param>
        public Setter(PerspexProperty property, IObservable<object> source)
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
        /// <remarks>
        /// Only one of <see cref="Source"/> and <see cref="Value"/> should be set.
        /// </remarks>
        public IObservable<object> Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        /// <remarks>
        /// Only one of <see cref="Source"/> and <see cref="Value"/> should be set.
        /// </remarks>
        public object Value
        {
            get;
            set;
        }
    }
}
