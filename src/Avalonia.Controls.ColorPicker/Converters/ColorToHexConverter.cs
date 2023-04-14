using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts a color to a hex string and vice versa.
    /// </summary>
    public class ColorToHexConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether the alpha component is visible in the Hex formatted text.
        /// </summary>
        /// <remarks>
        /// When hidden the existing alpha component value is maintained. Also when hidden the user is still
        /// able to input an 8-digit number with alpha. Alpha will be processed but then removed when displayed.
        ///
        /// Because this property only controls whether alpha is displayed (and it is still processed regardless)
        /// it is termed 'Visible' instead of 'Enabled'.
        /// </remarks>
        public bool IsAlphaVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets the position of a color's alpha component relative to all other components.
        /// </summary>
        public AlphaComponentPosition AlphaPosition { get; set; } = AlphaComponentPosition.Leading;

        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            Color color;
            bool includeSymbol = parameter as bool? ?? false;

            if (value is Color valueColor)
            {
                color = valueColor;
            }
            else if (value is HslColor valueHslColor)
            {
                color = valueHslColor.ToRgb();
            }
            else if (value is HsvColor valueHsvColor)
            {
                color = valueHsvColor.ToRgb();
            }
            else if (value is SolidColorBrush valueBrush)
            {
                color = valueBrush.Color;
            }
            else
            {
                // Invalid color value provided
                return AvaloniaProperty.UnsetValue;
            }

            return ToHexString(color, AlphaPosition, IsAlphaVisible, includeSymbol);
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            string hexValue = value?.ToString() ?? string.Empty;
            return ParseHexString(hexValue, AlphaPosition) ?? AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Converts the given color to its hex color value string representation.
        /// </summary>
        /// <param name="color">The color to represent as a hex value string.</param>
        /// <param name="alphaPosition">The output position of the alpha component.</param>
        /// <param name="includeAlpha">Whether the alpha component will be included in the hex string.</param>
        /// <param name="includeSymbol">Whether the hex symbol '#' will be added.</param>
        /// <returns>The input color converted to its hex value string.</returns>
        public static string ToHexString(
            Color color,
            AlphaComponentPosition alphaPosition,
            bool includeAlpha = true,
            bool includeSymbol = false)
        {
            uint intColor;
            string hexColor;

            if (includeAlpha)
            {
                if (alphaPosition == AlphaComponentPosition.Trailing)
                {
                    intColor = ((uint)color.R << 24) | ((uint)color.G << 16) | ((uint)color.B << 8) | (uint)color.A;
                }
                else
                {
                    // Default is Leading alpha (same as XAML)
                    intColor = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | (uint)color.B;
                }

                hexColor = intColor.ToString("x8", CultureInfo.InvariantCulture).ToUpperInvariant();
            }
            else
            {
                // In this case the alpha position no longer matters
                // Both cases are calculated the same
                intColor = ((uint)color.R << 16) | ((uint)color.G << 8) | (uint)color.B;
                hexColor = intColor.ToString("x6", CultureInfo.InvariantCulture).ToUpperInvariant();
            }

            if (includeSymbol)
            {
                hexColor = '#' + hexColor;
            }

            return hexColor;
        }

        /// <summary>
        /// Parses a hex color value string into a new <see cref="Color"/>.
        /// </summary>
        /// <param name="hexColor">The hex color string to parse.</param>
        /// <param name="alphaPosition">The input position of the alpha component.</param>
        /// <returns>The parsed <see cref="Color"/>; otherwise, null.</returns>
        public static Color? ParseHexString(
            string hexColor,
            AlphaComponentPosition alphaPosition)
        {
            hexColor = hexColor.Trim();

            if (!hexColor.StartsWith("#", StringComparison.Ordinal))
            {
                hexColor = "#" + hexColor;
            }

            if (TryParseHexFormat(hexColor.AsSpan(), alphaPosition, out Color color))
            {
                return color;
            }

            return null;
        }

        /// <summary>
        /// Parses the given span of characters representing a hex color value into a new <see cref="Color"/>.
        /// </summary>
        /// <remarks>
        /// This is based on the Color.TryParseHexFormat() method.
        /// It is copied because it needs to be extended to handle alpha position.
        /// However, the alpha position enum is only available in the controls namespace with the ColorPicker control.
        /// </remarks>
        private static bool TryParseHexFormat(
            ReadOnlySpan<char> s,
            AlphaComponentPosition alphaPosition,
            out Color color)
        {
            static bool TryParseCore(ReadOnlySpan<char> input, AlphaComponentPosition alphaPosition, ref Color color)
            {
                var alphaComponent = 0u;

                if (input.Length == 6)
                {
                    if (alphaPosition == AlphaComponentPosition.Trailing)
                    {
                        alphaComponent = 0x000000FF;
                    }
                    else
                    {
                        alphaComponent = 0xFF000000;
                    }
                }
                else if (input.Length != 8)
                {
                    return false;
                }

                if (!input.TryParseUInt(NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
                {
                    return false;
                }

                if (alphaComponent != 0)
                {
                    if (alphaPosition == AlphaComponentPosition.Trailing)
                    {
                        parsed = (parsed << 8) | alphaComponent;
                    }
                    else
                    {
                        parsed = parsed | alphaComponent;
                    }
                }

                if (alphaPosition == AlphaComponentPosition.Trailing)
                {
                    // #RRGGBBAA
                    color = new Color(
                        a: (byte)(parsed & 0xFF),
                        r: (byte)((parsed >> 24) & 0xFF),
                        g: (byte)((parsed >> 16) & 0xFF),
                        b: (byte)((parsed >> 8) & 0xFF));
                }
                else
                {
                    // #AARRGGBB
                    color = new Color(
                        a: (byte)((parsed >> 24) & 0xFF),
                        r: (byte)((parsed >> 16) & 0xFF),
                        g: (byte)((parsed >> 8) & 0xFF),
                        b: (byte)(parsed & 0xFF));
                }

                return true;
            }

            color = default;

            ReadOnlySpan<char> input = s.Slice(1);

            // Handle shorthand cases like #FFF (RGB) or #FFFF (ARGB).
            if (input.Length == 3 || input.Length == 4)
            {
                var extendedLength = 2 * input.Length;
                Span<char> extended = stackalloc char[extendedLength];

                for (int i = 0; i < input.Length; i++)
                {
                    extended[2 * i + 0] = input[i];
                    extended[2 * i + 1] = input[i];
                }

                return TryParseCore(extended, alphaPosition, ref color);
            }

            return TryParseCore(input, alphaPosition, ref color);
        }
    }
}
