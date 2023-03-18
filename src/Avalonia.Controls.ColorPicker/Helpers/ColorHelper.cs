using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Contains helpers useful when working with colors.
    /// </summary>
    public static class ColorHelper
    {
        private static readonly Dictionary<Color, string> _cachedDisplayNames = new Dictionary<Color, string>();
        private static readonly Dictionary<KnownColor, double> _cachedKnownColorHues = new Dictionary<KnownColor, double>();
        private static readonly Dictionary<KnownColor, string> _cachedKnownColorNames = new Dictionary<KnownColor, string>();
        private static readonly object _displayNameCacheMutex = new object();
        private static readonly object _knownColorCacheMutex = new object();

        /// <summary>
        /// Gets the relative (perceptual) luminance/brightness of the given color.
        /// 1 is closer to white while 0 is closer to black.
        /// </summary>
        /// <param name="color">The color to calculate relative luminance for.</param>
        /// <returns>The relative (perceptual) luminance/brightness of the given color.</returns>
        public static double GetRelativeLuminance(Color color)
        {
            // The equation for relative luminance is given by
            //
            // L = 0.2126 * Rg + 0.7152 * Gg + 0.0722 * Bg
            //
            // where Xg = { X/3294 if X <= 10, (R/269 + 0.0513)^2.4 otherwise }
            //
            // If L is closer to 1, then the color is closer to white; if it is closer to 0,
            // then the color is closer to black.  This is based on the fact that the human
            // eye perceives green to be much brighter than red, which in turn is perceived to be
            // brighter than blue.

            double rg = color.R <= 10 ? color.R / 3294.0 : Math.Pow(color.R / 269.0 + 0.0513, 2.4);
            double gg = color.G <= 10 ? color.G / 3294.0 : Math.Pow(color.G / 269.0 + 0.0513, 2.4);
            double bg = color.B <= 10 ? color.B / 3294.0 : Math.Pow(color.B / 269.0 + 0.0513, 2.4);

            return (0.2126 * rg + 0.7152 * gg + 0.0722 * bg);
        }

        /// <summary>
        /// Determines if color display names are supported based on the current thread culture.
        /// </summary>
        /// <remarks>
        /// Only English names are currently supported following known color names.
        /// In the future known color names could be localized.
        /// </remarks>
        public static bool ToDisplayNameExists
        {
            get => CultureInfo.CurrentUICulture.Name.StartsWith("EN", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines an approximate display name for the given color.
        /// </summary>
        /// <param name="color">The color to get the display name for.</param>
        /// <returns>The approximate color display name.</returns>
        public static string ToDisplayName(Color color)
        {
            // Without rounding, there are 16,777,216 possible RGB colors (without alpha).
            // This is too many to cache and search through for performance reasons.
            // It is also needlessly large as there are only ~140 known/named colors.
            // Therefore, rounding of the input color's component values is done to
            // reduce the color space into something more useful.
            //
            // The rounding value of 5 is specially chosen.
            // It is a factor of 255 and therefore evenly divisible which improves
            // the quality of the calculations.
            double rounding = 5;
            var roundedColor = new Color(
                0xFF,
                Convert.ToByte(Math.Round(color.R / rounding) * rounding),
                Convert.ToByte(Math.Round(color.G / rounding) * rounding),
                Convert.ToByte(Math.Round(color.B / rounding) * rounding));
            var hsvColor = color.ToHsv();

            // Handle extremes that are outside the below algorithm
            if (color.A == 0x00)
            {
                return GetDisplayName(KnownColor.Transparent);
            }
            else if (hsvColor.S <= 0.0)
            {
                return GetDisplayName(KnownColor.White);
            }
            else if (hsvColor.V <= 0.0)
            {
                return GetDisplayName(KnownColor.Black);
            }

            // Attempt to use a previously cached display name
            lock (_displayNameCacheMutex)
            {
                if (_cachedDisplayNames.TryGetValue(roundedColor, out var displayName))
                {
                    return displayName;
                }
            }

            // Build KnownColor caches if they don't already exist
            lock (_knownColorCacheMutex)
            {
                if (_cachedKnownColorHues.Count == 0 ||
                    _cachedKnownColorNames.Count == 0)
                {
                    _cachedKnownColorHues.Clear();
                    _cachedKnownColorNames.Clear();

                    var knownColors = (KnownColor[])Enum.GetValues(typeof(KnownColor));
                    for (int i = 1; i < knownColors.Length; i++) // Skip 'None' so start at 1
                    {
                        KnownColor knownColor = knownColors[i];

                        // Transparent is skipped since alpha is ignored making it equivalent to White
                        if (knownColor == KnownColor.Transparent)
                        {
                            continue;
                        }

                        double hue = KnownColors.ToColor(knownColor).ToHsv().H;

                        // Some known colors have the same numerical value. For example:
                        //  - Aqua = 0xff00ffff
                        //  - Cyan = 0xff00ffff
                        //
                        // This is not possible to represent in a dictionary which requires
                        // unique values. Therefore, only the first value is used.

                        if (!_cachedKnownColorHues.ContainsKey(knownColor))
                        {
                            _cachedKnownColorHues.Add(knownColor, hue);
                        }

                        if (!_cachedKnownColorNames.ContainsKey(knownColor))
                        {
                            _cachedKnownColorNames.Add(knownColor, GetDisplayName(knownColor));
                        }
                    }
                }
            }

            // Find the closest known color by finding nearest Hue
            // Since Hue is the best measure of human perception of the color itself
            // it is not necessary to check other components (Saturation, Value).
            var closestKnownColor = KnownColor.None;
            var closestKnownColorHueDiff = double.PositiveInfinity;

            lock (_knownColorCacheMutex)
            {
                foreach (var hueEntry in _cachedKnownColorHues)
                {
                    // Closest hue before or after is allowed
                    // Therefore, use an absolute value
                    double difference = Math.Abs(hsvColor.H - hueEntry.Value);

                    if (difference < closestKnownColorHueDiff)
                    {
                        closestKnownColor = hueEntry.Key;
                        closestKnownColorHueDiff = difference;
                    }
                }
            }

            // Return the closest known color as the display name
            // Cache results for next time as well
            if (closestKnownColor != KnownColor.None)
            {
                string displayName;

                lock (_knownColorCacheMutex)
                {
                    if (!_cachedKnownColorNames.TryGetValue(closestKnownColor, out displayName))
                    {
                        displayName = GetDisplayName(closestKnownColor);
                    }
                }

                lock (_displayNameCacheMutex)
                {
                    _cachedDisplayNames.Add(roundedColor, displayName);
                }

                return displayName;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the human-readable display name for the given <see cref="KnownColor"/>.
        /// </summary>
        /// <remarks>
        /// This currently uses the <see cref="KnownColor"/> enum value's C# name directly
        /// which limits it to the EN language only. In the future this should be localized
        /// to other cultures.
        /// </remarks>
        /// <param name="knownColor">The <see cref="KnownColor"/> to get the display name for.</param>
        /// <returns>The human-readable display name for the given <see cref="KnownColor"/>.</returns>
        private static string GetDisplayName(KnownColor knownColor)
        {
            var sb = StringBuilderCache.Acquire();
            string name = knownColor.ToString();

            // Add spaces converting PascalCase to human-readable names
            for (int i = 0; i < name.Length; i++)
            {
                if (i != 0 &&
                    char.IsUpper(name[i]))
                {
                    sb.Append(' ');
                }

                sb.Append(name[i]);
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}
