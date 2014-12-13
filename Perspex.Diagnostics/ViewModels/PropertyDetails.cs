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
        private object value;

        public PropertyDetails(PerspexPropertyValue value)
        {
            this.Name = value.Property.Name;
            this.value = value.CurrentValue ?? "(null)";
            this.Priority = (value.PriorityValue != null) ?
                Enum.GetName(typeof(BindingPriority), value.PriorityValue.ValuePriority) :
                value.Property.Inherits ? "Inherited" : "Unset";

            if (value.PriorityValue != null)
            {
                value.PriorityValue.Changed.Subscribe(x => this.Value = x.Item2);
            }
        }

        public string Name
        {
            get;
            private set;
        }

        public object Value
        {
            get { return this.value; }
            private set { this.RaiseAndSetIfChanged(ref this.value, value); }
        }

        public string Priority
        {
            get;
            private set;
        }
    }
}
