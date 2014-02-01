// -----------------------------------------------------------------------
// <copyright file="Setter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Diagnostics.Contracts;
    using Perspex.Controls;

    public class Setter
    {
        private object oldValue;

        public PerspexProperty Property
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }

        public void Apply(Control control)
        {
            Contract.Requires<NullReferenceException>(control != null);

            this.oldValue = control.GetValue(this.Property);
            control.SetValue(this.Property, this.Value);
        }

        public void Unapply(Control control)
        {
            Contract.Requires<NullReferenceException>(control != null);

            control.SetValue(this.Property, this.oldValue);
        }
    }
}
