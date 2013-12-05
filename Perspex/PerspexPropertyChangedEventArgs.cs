// -----------------------------------------------------------------------
// <copyright file="PerspexPropertyChangedEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides information for a perspex property change.
    /// </summary>
    public class PerspexPropertyChangedEventArgs
    {
        public PerspexPropertyChangedEventArgs(
            PerspexProperty property,
            object oldValue,
            object newValue)
        {
            this.Property = property;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        public PerspexProperty Property { get; private set; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public object OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public object NewValue { get; private set; }
    }
}
