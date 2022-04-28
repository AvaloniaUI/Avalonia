using System;
using System.Globalization;
using System.Collections.Generic;
using Avalonia.Media;
using System.Text;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Contains helpers useful when working with color names.
    /// </summary>
    public static class ColorNameHelpers
    {
        private static readonly Dictionary<Color, string> cachedDisplayNames = new Dictionary<Color, string>();
        private static readonly object cacheMutex = new object();

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
            double rounding = 5;
            var roundedColor = new Color(
                0xFF,
                Convert.ToByte(Math.Round(color.R / rounding) * rounding),
                Convert.ToByte(Math.Round(color.G / rounding) * rounding),
                Convert.ToByte(Math.Round(color.B / rounding) * rounding));

            // Attempt to use a previously cached display name
            lock (cacheMutex)
            {
                if (cachedDisplayNames.TryGetValue(roundedColor, out var displayName))
                {
                    return displayName;
                }
            }

            // Find the closest known color by measuring 3D Euclidean distance (ignore alpha)
            var closestKnownColor = KnownColor.None;
            var closestKnownColorDistance = double.PositiveInfinity;
            var knownColors = (KnownColor[])Enum.GetValues(typeof(KnownColor));

            for (int i = 1; i < knownColors.Length; i++) // Skip 'None'
            {
                // Transparent is skipped since alpha is ignored making it equivalent to White
                if (knownColors[i] != KnownColor.Transparent)
                {
                    Color knownColor = KnownColors.ToColor(knownColors[i]);

                    double distance = Math.Sqrt(
                        Math.Pow((double)(roundedColor.R - knownColor.R), 2.0) +
                        Math.Pow((double)(roundedColor.G - knownColor.G), 2.0) +
                        Math.Pow((double)(roundedColor.B - knownColor.B), 2.0));

                    if (distance < closestKnownColorDistance)
                    {
                        closestKnownColor = knownColors[i];
                        closestKnownColorDistance = distance;
                    }
                }
            }

            // Return the closest known color as the display name
            // Cache results for next time as well
            if (closestKnownColor != KnownColor.None)
            {
                StringBuilder sb = new StringBuilder(); 
                string name = closestKnownColor.ToString();

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

                string displayName = sb.ToString();

                lock (cacheMutex)
                {
                    cachedDisplayNames.Add(roundedColor, displayName);
                }

                return displayName;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
