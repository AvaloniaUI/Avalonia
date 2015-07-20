namespace Perspex.Controls.Primitives
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Perspex.Controls.Templates;

    public abstract class RangeBase : TemplatedControl
    {
        public static readonly PerspexProperty<double> MinimumProperty =
    PerspexProperty.Register<RangeBase, double>("Minimum");

        public static readonly PerspexProperty<double> MaximumProperty =
            PerspexProperty.Register<RangeBase, double>("Maximum", defaultValue: 100.0);

        public static readonly PerspexProperty<double> ValueProperty =
            PerspexProperty.Register<RangeBase, double>("Value");

        public double Minimum
        {
            get { return this.GetValue(MinimumProperty); }
            set { this.SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return this.GetValue(MaximumProperty); }
            set { this.SetValue(MaximumProperty, value); }
        }

        public double Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}
