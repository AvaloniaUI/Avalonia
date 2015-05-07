// --------------------------------------------------------------------
// <copyright file="PerspexPropertyValue.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    public class PerspexPropertyValue
    {
        public PerspexPropertyValue(
            PerspexProperty property, 
            object value,
            BindingPriority priority)
        {
            this.Property = property;
            this.Value = value;
            this.Priority = priority;
        }

        public PerspexProperty Property { get; private set; }

        public object Value { get; private set; }

        public BindingPriority Priority { get; private set; }
    }
}
