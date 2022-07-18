using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Primitives.Converters
{
    /// <summary>
    /// Gets the third <see cref="ColorComponent"/> corresponding with a given
    /// <see cref="ColorSpectrumComponents"/> that represents the other two components.
    /// </summary>
    /// <remarks>
    /// This is a highly-specialized converter for the color picker.
    /// </remarks>
    public class ThirdComponentConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is ColorSpectrumComponents components)
            {
                // Note: Alpha is not relevant here
                switch (components)
                {
                    case ColorSpectrumComponents.HueSaturation:
                    case ColorSpectrumComponents.SaturationHue:
                        return (ColorComponent)HsvComponent.Value;
                    case ColorSpectrumComponents.HueValue:
                    case ColorSpectrumComponents.ValueHue:
                        return (ColorComponent)HsvComponent.Saturation;
                    case ColorSpectrumComponents.SaturationValue:
                    case ColorSpectrumComponents.ValueSaturation:
                        return (ColorComponent)HsvComponent.Hue;
                }
            }

            return AvaloniaProperty.UnsetValue;
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }
}
