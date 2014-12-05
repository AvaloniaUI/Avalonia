// -----------------------------------------------------------------------
// <copyright file="PropertyDetails.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using ReactiveUI;

    internal class PropertyDetails : ReactiveObject
    {
        public PropertyDetails(PerspexPropertyValue value)
        {
            this.Name = value.Property.Name;
            this.Value = value.CurrentValue ?? "(null)";
            this.Priority = (value.PriorityValue != null) ?
                Enum.GetName(typeof(BindingPriority), value.PriorityValue.ValuePriority) :
                value.Property.Inherits ? "Inherited" : "Unset";
        }

        public string Name
        {
            get;
            private set;
        }

        public object Value
        {
            get;
            private set;
        }

        public string Priority
        {
            get;
            private set;
        }
    }
}
