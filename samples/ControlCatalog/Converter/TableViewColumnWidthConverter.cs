using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace ControlCatalog.Converter;

public sealed class TableViewColumnWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool useStarSize &&
            parameter is string stringParameter &&
            double.TryParse(stringParameter, NumberStyles.Number, CultureInfo.InvariantCulture, out var baseWidth))
        {
            return useStarSize ?
                new GridLength(baseWidth, GridUnitType.Star) :
                new GridLength(baseWidth * 100, GridUnitType.Pixel);
        }

        return AvaloniaProperty.UnsetValue;
    }

    object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
