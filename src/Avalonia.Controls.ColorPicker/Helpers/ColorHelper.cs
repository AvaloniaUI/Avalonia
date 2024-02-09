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
        private static readonly Dictionary<HsvColor, string> _cachedDisplayNames = new Dictionary<HsvColor, string>();
        private static readonly Dictionary<KnownColor, string> _cachedKnownColorNames = new Dictionary<KnownColor, string>();
        private static readonly object _displayNameCacheMutex = new object();
        private static readonly object _knownColorCacheMutex = new object();
        private static readonly KnownColor[] _knownColors =
#if NET6_0_OR_GREATER
            Enum.GetValues<KnownColor>();
#else 
            (KnownColor[])Enum.GetValues(typeof(KnownColor));
#endif

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
            var hsvColor = color.ToHsv();

            // Handle extremes that are outside the below algorithm
            if (color.A == 0x00)
            {
                return GetDisplayName(KnownColor.Transparent);
            }

            // HSV ----------------------------------------------------------------------
            //
            // There are far too many possible HSV colors to cache and search through
            // for performance reasons. Therefore, the HSV color is rounded.
            // Rounding is tolerable in this algorithm because it is perception based.
            // Hue is the most important for user perception so is rounded the least.
            // Then there is a lot of loss in rounding the saturation and value components
            // which are not as closely related to perceived color.
            //
            //         Hue : Round to nearest int (0..360)
            //  Saturation : Round to the nearest 1/10 (0..1)
            //       Value : Round to the nearest 1/10 (0..1)
            //       Alpha : Is ignored in this algorithm
            //
            // Rounding results in ~36_000 values to cache in the worse case.
            //
            // RGB ----------------------------------------------------------------------
            //
            // The original algorithm worked in RGB color space.
            // If this code is every adjusted to work in RGB again note the following:
            //
            // Without rounding, there are 16_777_216 possible RGB colors (without alpha).
            // This is too many to cache and search through for performance reasons.
            // It is also needlessly large as there are only ~140 known/named colors.
            // Therefore, rounding of the input color's component values is done to
            // reduce the color space into something more useful.
            //
            // The rounding value of 5 is specially chosen.
            // It is a factor of 255 and therefore evenly divisible which improves
            // the quality of the calculations.
            var roundedHsvColor = new HsvColor(
                1.0,
                Math.Round(hsvColor.H, 0),
                Math.Round(hsvColor.S, 1),
                Math.Round(hsvColor.V, 1));

            // Attempt to use a previously cached display name
            lock (_displayNameCacheMutex)
            {
                if (_cachedDisplayNames.TryGetValue(roundedHsvColor, out var displayName))
                {
                    return displayName;
                }
            }

            // Build the KnownColor name cache if it doesn't already exist
            lock (_knownColorCacheMutex)
            {
                if (_cachedKnownColorNames.Count == 0)
                {
                    for (int i = 1; i < _knownColors.Length; i++) // Skip 'None' so start at 1
                    {
                        KnownColor knownColor = _knownColors[i];

                        // Some known colors have the same numerical value. For example:
                        //  - Aqua = 0xff00ffff
                        //  - Cyan = 0xff00ffff
                        //
                        // This is not possible to represent in a dictionary which requires
                        // unique values. Therefore, only the first value is used.

                        if (!_cachedKnownColorNames.ContainsKey(knownColor))
                        {
                            _cachedKnownColorNames.Add(knownColor, GetDisplayName(knownColor));
                        }
                    }
                }
            }

            // Find the closest known color by measuring 3D Euclidean distance (ignore alpha)
            // This is done in HSV color space to most closely match user-perception
            var closestKnownColor = KnownColor.None;
            var closestKnownColorDistance = double.PositiveInfinity;

            for (int i = 1; i < _knownColors.Length; i++) // Skip 'None' so start at 1
            {
                KnownColor knownColor = _knownColors[i];

                // Transparent is skipped since alpha is ignored making it equivalent to White
                if (knownColor != KnownColor.Transparent)
                {
                    HsvColor knownHsvColor = KnownColors.ToColor(knownColor).ToHsv();

                    double distance = Math.Sqrt(
                        Math.Pow((roundedHsvColor.H - knownHsvColor.H), 2.0) +
                        Math.Pow((roundedHsvColor.S - knownHsvColor.S), 2.0) +
                        Math.Pow((roundedHsvColor.V - knownHsvColor.V), 2.0));

                    if (distance < closestKnownColorDistance)
                    {
                        closestKnownColor = knownColor;
                        closestKnownColorDistance = distance;
                    }
                }
            }

            // Return the closest known color as the display name
            // Cache results for next time as well
            if (closestKnownColor != KnownColor.None)
            {
                string? displayName;

                lock (_knownColorCacheMutex)
                {
                    if (!_cachedKnownColorNames.TryGetValue(closestKnownColor, out displayName))
                    {
                        displayName = GetDisplayName(closestKnownColor);
                    }
                }

                lock (_displayNameCacheMutex)
                {
                    _cachedDisplayNames.Add(roundedHsvColor, displayName);
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
