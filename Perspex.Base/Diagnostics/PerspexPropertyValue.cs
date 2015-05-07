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
            BindingPriority priority,
            string diagnostic)
        {
            this.Property = property;
            this.Value = value;
            this.Priority = priority;
            this.Diagnostic = diagnostic;
        }

        public PerspexProperty Property { get; }

        public object Value { get; }

        public BindingPriority Priority { get; }

        public string Diagnostic { get; }
    }
}
