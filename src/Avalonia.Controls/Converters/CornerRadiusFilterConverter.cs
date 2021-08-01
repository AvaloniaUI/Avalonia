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
        public CornerRadiusFilterKinds Filter { get; set; }

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

            return new CornerRadius(
                Filter.HasAllFlags(CornerRadiusFilterKinds.TopLeft) ? radius.TopLeft * Scale : 0,
                Filter.HasAllFlags(CornerRadiusFilterKinds.TopRight) ? radius.TopRight * Scale : 0,
                Filter.HasAllFlags(CornerRadiusFilterKinds.BottomRight) ? radius.BottomRight * Scale : 0,
                Filter.HasAllFlags(CornerRadiusFilterKinds.BottomLeft) ? radius.BottomLeft * Scale : 0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
