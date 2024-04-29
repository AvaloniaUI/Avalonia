using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Converters;

internal class BrushSelectorConveter : AvaloniaObject, IValueConverter
{
    public static readonly DirectProperty<BrushSelectorConveter, IBrush?> BrushProperty =
        AvaloniaProperty.RegisterDirect<BrushSelectorConveter, IBrush?>(nameof(Brush)
            , o => o.Brush
            , (o, v) => o.Brush = v);

    public IBrush? Brush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (ReferenceEquals(value, parameter))
        {
            return Brush;
        }
        else if (value is ISolidColorBrush a
            && parameter is ISolidColorBrush b
            && a.Color == b.Color
            && a.Transform == b.Transform
            && b.Opacity == a.Opacity
            )
        {
            return Brush;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
