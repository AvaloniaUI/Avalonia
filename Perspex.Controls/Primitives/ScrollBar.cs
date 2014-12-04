// -----------------------------------------------------------------------
// <copyright file="ScrollBar.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Reactive.Linq;

    public class ScrollBar : TemplatedControl
    {
        public static readonly PerspexProperty<double> MinimumProperty =
            PerspexProperty.Register<ScrollBar, double>("Minimum");

        public static readonly PerspexProperty<double> MaximumProperty =
            PerspexProperty.Register<ScrollBar, double>("Maximum", defaultValue: 100.0);

        public static readonly PerspexProperty<double> ValueProperty =
            PerspexProperty.Register<ScrollBar, double>("Value");

        public static readonly PerspexProperty<double> ViewportSizeProperty =
            PerspexProperty.Register<ScrollBar, double>("ViewportSize", defaultValue: double.NaN);

        public static readonly PerspexProperty<Orientation> OrientationProperty =
            PerspexProperty.Register<ScrollBar, Orientation>("Orientation");

        static ScrollBar()
        {
            Control.PseudoClass(OrientationProperty, x => x == Orientation.Horizontal, ":horizontal");
            Control.PseudoClass(OrientationProperty, x => x == Orientation.Vertical, ":vertical");
        }

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

        public double ViewportSize
        {
            get { return this.GetValue(ViewportSizeProperty); }
            set { this.SetValue(ViewportSizeProperty, value); }
        }

        public Orientation Orientation
        {
            get { return this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }
    }
}
