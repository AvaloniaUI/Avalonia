using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace ControlCatalog
{
    public class MyControl : Grid, IStyleable
    {
        Type IStyleable.StyleKey => typeof(Grid);

        /// <summary>
        /// Defines the Proportion attached property.
        /// </summary>
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
