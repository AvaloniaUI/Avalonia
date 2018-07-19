using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog
{
    public class MyControl : Control
    {
        public static readonly AttachedProperty<double> ProportionProperty =
            AvaloniaProperty.RegisterAttached<MyControl, IControl, double>("Proportion", double.NaN);

        /// <summary>
        /// Gets the value of the Proportion attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The Proportion attached property.</returns>
        public static double GetProportion(IControl control)
        {
            return control.GetValue(ProportionProperty);
        }

        /// <summary>
        /// Sets the value of the Proportion attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the Dock property.</param>
        public static void SetProportion(IControl control, double value)
        {
            control.SetValue(ProportionProperty, value);
        }
    }
}
