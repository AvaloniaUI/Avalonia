using System;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class TestControl : Control
    {
        public static readonly StyledProperty<double> DoubleProperty =
            AttachedPropertyOwner.DoubleProperty.AddOwner<TestControl>();

        public double Double
        {
            get => GetValue(DoubleProperty);
            set => SetValue(DoubleProperty, value);
        }
    }
}
