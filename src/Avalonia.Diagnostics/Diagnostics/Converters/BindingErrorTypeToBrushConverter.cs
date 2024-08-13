using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Converters;

internal class BindingErrorTypeToBrushConverter : IValueConverter
{
    public IBrush? None { get; set; }
    public IBrush? DataValidationError { get; set; }
    public IBrush? Error {  get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            BindingErrorType.None => None,
            BindingErrorType.DataValidationError => DataValidationError,
            BindingErrorType.Error => Error,
            _ => throw new NotSupportedException()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}
