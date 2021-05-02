#nullable enable
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts an existing CornerRadius struct to a new CornerRadius struct,
    /// with filters applied to extract only the specified fields, leaving the others set to 0.
    /// </summary>
    public class CornerRadiusFilterConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the type of the filter applied to the <see cref="CornerRadiusFilterConverter"/>.
        /// </summary>
        public CornerRadiusFilterKind Filter { get; set; }

        /// <summary>
        /// Gets or sets the scale multiplier applied to the <see cref="CornerRadiusFilterConverter"/>.
        /// </summary>
        public double Scale { get; set; } = 1;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is CornerRadius radius))
            {
                return value;
            }

            if (Filter == CornerRadiusFilterKind.TopLeftValue
                || Filter == CornerRadiusFilterKind.BottomRightValue)
            {
                var doubleValue = GetDoubleValue(radius, Filter);
                return double.IsNaN(Scale) ? doubleValue : doubleValue * Scale;
            }
            else
            {
                var cornerRadius = GetCornerRadiusValue(radius, Filter);
                return double.IsNaN(Scale) ? cornerRadius : new CornerRadius(
                    cornerRadius.TopLeft * Scale,
                    cornerRadius.TopRight * Scale,
                    cornerRadius.BottomRight * Scale,
                    cornerRadius.BottomLeft * Scale);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private CornerRadius GetCornerRadiusValue(CornerRadius radius, CornerRadiusFilterKind filterKind)
        {
            return filterKind switch
            {
                CornerRadiusFilterKind.Top => new CornerRadius(radius.TopLeft, radius.TopRight, 0, 0),
                CornerRadiusFilterKind.Right => new CornerRadius(0, radius.TopRight, radius.BottomRight, 0),
                CornerRadiusFilterKind.Bottom => new CornerRadius(0, 0, radius.BottomRight, radius.BottomLeft),
                CornerRadiusFilterKind.Left => new CornerRadius(radius.TopLeft, 0, 0, radius.BottomLeft),
                _ => radius,
            };
        }

        private double GetDoubleValue(CornerRadius radius, CornerRadiusFilterKind filterKind)
        {
            return filterKind switch
            {
                CornerRadiusFilterKind.TopLeftValue => radius.TopLeft,
                CornerRadiusFilterKind.BottomRightValue => radius.BottomRight,
                _ => 0,
            };
        }
    }
}
