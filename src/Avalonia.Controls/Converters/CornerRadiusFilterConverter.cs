using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts an existing CornerRadius struct to a new CornerRadius struct,
    /// with filters applied to extract only the specified corners, leaving the others set to 0.
    /// </summary>
    public class CornerRadiusFilterConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the corners to filter by.
        /// Only the specified corners will be included in the converted <see cref="CornerRadius"/>.
        /// </summary>
        public Corners Filter { get; set; }

        /// <summary>
        /// Gets or sets the scale multiplier applied uniformly to each corner.
        /// </summary>
        public double Scale { get; set; } = 1;

        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (!(value is CornerRadius radius))
            {
                return value;
            }

            return new CornerRadius(
                Filter.HasAllFlags(Corners.TopLeft) ? radius.TopLeft * Scale : 0,
                Filter.HasAllFlags(Corners.TopRight) ? radius.TopRight * Scale : 0,
                Filter.HasAllFlags(Corners.BottomRight) ? radius.BottomRight * Scale : 0,
                Filter.HasAllFlags(Corners.BottomLeft) ? radius.BottomLeft * Scale : 0);
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
