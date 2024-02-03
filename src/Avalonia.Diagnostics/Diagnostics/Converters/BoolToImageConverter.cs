using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Converters;

internal class BoolToImageConverter : IValueConverter
{
    public IImage? TrueImage { get; set; }

    public IImage? FalseImage { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            true => TrueImage,
            false => FalseImage,
            _ => null
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}
