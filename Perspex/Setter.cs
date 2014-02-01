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

            System.Diagnostics.Debug.WriteLine(
                string.Format("{0} Style Set {1}.{2}={3}",
                control.GetHashCode(),
                control.GetType().Name,
                this.Property.Name,
                this.Value));
        }

        public void Unapply(Control control)
        {
            Contract.Requires<NullReferenceException>(control != null);

            control.SetValue(this.Property, this.oldValue);
            
            System.Diagnostics.Debug.WriteLine(
                string.Format("{0} Style Unset {1}.{2}={3}",
                control.GetHashCode(),
                control.GetType().Name,
                this.Property.Name,
                oldValue));
        }
    }
}
