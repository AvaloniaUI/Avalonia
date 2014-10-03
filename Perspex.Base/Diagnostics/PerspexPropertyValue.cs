// -----------------------------------------------------------------------
// <copyright file="PerspexPropertyValue.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    public class PerspexPropertyValue
    {
        public PerspexPropertyValue(PerspexProperty property, object value)
        {
            this.Property = property;
            this.CurrentValue = value;
        }

        public PerspexPropertyValue(PerspexProperty property, PriorityValue priorityValue)
        {
            this.Property = property;
            this.CurrentValue = priorityValue.Value;
            this.PriorityValue = priorityValue;
        }

        public PerspexProperty Property { get; private set; }

        public object CurrentValue { get; private set; }

        public PriorityValue PriorityValue { get; private set; }
    }
}
