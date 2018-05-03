using System;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class AttachedPropertyOwner
    {
        public static readonly AttachedProperty<double> DoubleProperty =
            AvaloniaProperty.RegisterAttached<AttachedPropertyOwner, Control, double>("Double");

        public static double GetDouble(Control control) => control.GetValue(DoubleProperty);
        public static void SetDouble(Control control, double value) => control.SetValue(DoubleProperty, value);
    }
}
