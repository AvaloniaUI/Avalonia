using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements a reduced version of the 2014 Material Design color palette.
    /// </summary>
    /// <remarks>
    /// This palette is based on the one outlined here:
    ///
    ///   https://material.io/design/color/the-color-system.html#tools-for-picking-colors
    ///
    /// In order to make the palette uniform and rectangular the following
    /// alterations were made:
    ///
    ///  1. The A100-A700 shades of each color are excluded.
    ///     These shades do not exist for all colors (brown/gray).
    ///  2. Black/White are stand-alone and are also excluded.
    ///
    /// </remarks>
    public class MaterialColorPalette : IColorPalette
    {
        // See: https://material.io/design/color/the-color-system.html#tools-for-picking-colors
        // This is a reduced palette for uniformity
        private static Color[,]? _colorChart = null;
        private static int _colorChartColorCount = 0;
        private static int _colorChartShadeCount = 0;
        private static object _colorChartMutex = new object();

        /// <summary>
        /// Initializes all color chart colors.
        /// </summary>
        /// <remarks>
        /// This is pulled out separately to lazy load for performance.
        /// If no material color palette is ever used, no colors will be created.
        /// </remarks>
        private void InitColorChart()
        {
            lock (_colorChartMutex)
            {
                _colorChart = new Color[,]
                {
                    // Red
                    {
                        Color.FromArgb(0xFF, 0xFF, 0xEB, 0xEE),
                        Color.FromArgb(0xFF, 0xFF, 0xCD, 0xD2),
                        Color.FromArgb(0xFF, 0xEF, 0x9A, 0x9A),
                        Color.FromArgb(0xFF, 0xE5, 0x73, 0x73),
                        Color.FromArgb(0xFF, 0xEF, 0x53, 0x50),
                        Color.FromArgb(0xFF, 0xF4, 0x43, 0x36),
                        Color.FromArgb(0xFF, 0xE5, 0x39, 0x35),
                        Color.FromArgb(0xFF, 0xD3, 0x2F, 0x2F),
                        Color.FromArgb(0xFF, 0xC6, 0x28, 0x28),
                        Color.FromArgb(0xFF, 0xB7, 0x1C, 0x1C),
                    },

                    // Pink
                    {
                        Color.FromArgb(0xFF, 0xFC, 0xE4, 0xEC),
                        Color.FromArgb(0xFF, 0xF8, 0xBB, 0xD0),
                        Color.FromArgb(0xFF, 0xF4, 0x8F, 0xB1),
                        Color.FromArgb(0xFF, 0xF0, 0x62, 0x92),
                        Color.FromArgb(0xFF, 0xEC, 0x40, 0x7A),
                        Color.FromArgb(0xFF, 0xE9, 0x1E, 0x63),
                        Color.FromArgb(0xFF, 0xD8, 0x1B, 0x60),
                        Color.FromArgb(0xFF, 0xC2, 0x18, 0x5B),
                        Color.FromArgb(0xFF, 0xAD, 0x14, 0x57),
                        Color.FromArgb(0xFF, 0x88, 0x0E, 0x4F),
                    },

                    // Purple
                    {
                        Color.FromArgb(0xFF, 0xF3, 0xE5, 0xF5),
                        Color.FromArgb(0xFF, 0xE1, 0xBE, 0xE7),
                        Color.FromArgb(0xFF, 0xCE, 0x93, 0xD8),
                        Color.FromArgb(0xFF, 0xBA, 0x68, 0xC8),
                        Color.FromArgb(0xFF, 0xAB, 0x47, 0xBC),
                        Color.FromArgb(0xFF, 0x9C, 0x27, 0xB0),
                        Color.FromArgb(0xFF, 0x8E, 0x24, 0xAA),
                        Color.FromArgb(0xFF, 0x7B, 0x1F, 0xA2),
                        Color.FromArgb(0xFF, 0x6A, 0x1B, 0x9A),
                        Color.FromArgb(0xFF, 0x4A, 0x14, 0x8C),
                    },

                    // Deep Purple
                    {
                        Color.FromArgb(0xFF, 0xED, 0xE7, 0xF6),
                        Color.FromArgb(0xFF, 0xD1, 0xC4, 0xE9),
                        Color.FromArgb(0xFF, 0xB3, 0x9D, 0xDB),
                        Color.FromArgb(0xFF, 0x95, 0x75, 0xCD),
                        Color.FromArgb(0xFF, 0x7E, 0x57, 0xC2),
                        Color.FromArgb(0xFF, 0x67, 0x3A, 0xB7),
                        Color.FromArgb(0xFF, 0x5E, 0x35, 0xB1),
                        Color.FromArgb(0xFF, 0x51, 0x2D, 0xA8),
                        Color.FromArgb(0xFF, 0x45, 0x27, 0xA0),
                        Color.FromArgb(0xFF, 0x31, 0x1B, 0x92),
                    },

                    // Indigo
                    {
                        Color.FromArgb(0xFF, 0xE8, 0xEA, 0xF6),
                        Color.FromArgb(0xFF, 0xC5, 0xCA, 0xE9),
                        Color.FromArgb(0xFF, 0x9F, 0xA8, 0xDA),
                        Color.FromArgb(0xFF, 0x79, 0x86, 0xCB),
                        Color.FromArgb(0xFF, 0x5C, 0x6B, 0xC0),
                        Color.FromArgb(0xFF, 0x3F, 0x51, 0xB5),
                        Color.FromArgb(0xFF, 0x39, 0x49, 0xAB),
                        Color.FromArgb(0xFF, 0x30, 0x3F, 0x9F),
                        Color.FromArgb(0xFF, 0x28, 0x35, 0x93),
                        Color.FromArgb(0xFF, 0x1A, 0x23, 0x7E),
                    },

                    // Blue
                    {
                        Color.FromArgb(0xFF, 0xE3, 0xF2, 0xFD),
                        Color.FromArgb(0xFF, 0xBB, 0xDE, 0xFB),
                        Color.FromArgb(0xFF, 0x90, 0xCA, 0xF9),
                        Color.FromArgb(0xFF, 0x64, 0xB5, 0xF6),
                        Color.FromArgb(0xFF, 0x42, 0xA5, 0xF5),
                        Color.FromArgb(0xFF, 0x21, 0x96, 0xF3),
                        Color.FromArgb(0xFF, 0x1E, 0x88, 0xE5),
                        Color.FromArgb(0xFF, 0x19, 0x76, 0xD2),
                        Color.FromArgb(0xFF, 0x15, 0x65, 0xC0),
                        Color.FromArgb(0xFF, 0x0D, 0x47, 0xA1),
                    },

                    // Light Blue
                    {
                        Color.FromArgb(0xFF, 0xE1, 0xF5, 0xFE),
                        Color.FromArgb(0xFF, 0xB3, 0xE5, 0xFC),
                        Color.FromArgb(0xFF, 0x81, 0xD4, 0xFA),
                        Color.FromArgb(0xFF, 0x4F, 0xC3, 0xF7),
                        Color.FromArgb(0xFF, 0x29, 0xB6, 0xF6),
                        Color.FromArgb(0xFF, 0x03, 0xA9, 0xF4),
                        Color.FromArgb(0xFF, 0x03, 0x9B, 0xE5),
                        Color.FromArgb(0xFF, 0x02, 0x88, 0xD1),
                        Color.FromArgb(0xFF, 0x02, 0x77, 0xBD),
                        Color.FromArgb(0xFF, 0x01, 0x57, 0x9B),
                    },

                    // Cyan
                    {
                        Color.FromArgb(0xFF, 0xE0, 0xF7, 0xFA),
                        Color.FromArgb(0xFF, 0xB2, 0xEB, 0xF2),
                        Color.FromArgb(0xFF, 0x80, 0xDE, 0xEA),
                        Color.FromArgb(0xFF, 0x4D, 0xD0, 0xE1),
                        Color.FromArgb(0xFF, 0x26, 0xC6, 0xDA),
                        Color.FromArgb(0xFF, 0x00, 0xBC, 0xD4),
                        Color.FromArgb(0xFF, 0x00, 0xAC, 0xC1),
                        Color.FromArgb(0xFF, 0x00, 0x97, 0xA7),
                        Color.FromArgb(0xFF, 0x00, 0x83, 0x8F),
                        Color.FromArgb(0xFF, 0x00, 0x60, 0x64),
                    },

                    // Teal
                    {
                        Color.FromArgb(0xFF, 0xE0, 0xF2, 0xF1),
                        Color.FromArgb(0xFF, 0xB2, 0xDF, 0xDB),
                        Color.FromArgb(0xFF, 0x80, 0xCB, 0xC4),
                        Color.FromArgb(0xFF, 0x4D, 0xB6, 0xAC),
                        Color.FromArgb(0xFF, 0x26, 0xA6, 0x9A),
                        Color.FromArgb(0xFF, 0x00, 0x96, 0x88),
                        Color.FromArgb(0xFF, 0x00, 0x89, 0x7B),
                        Color.FromArgb(0xFF, 0x00, 0x79, 0x6B),
                        Color.FromArgb(0xFF, 0x00, 0x69, 0x5C),
                        Color.FromArgb(0xFF, 0x00, 0x4D, 0x40),
                    },

                    // Green
                    {
                        Color.FromArgb(0xFF, 0xE8, 0xF5, 0xE9),
                        Color.FromArgb(0xFF, 0xC8, 0xE6, 0xC9),
                        Color.FromArgb(0xFF, 0xA5, 0xD6, 0xA7),
                        Color.FromArgb(0xFF, 0x81, 0xC7, 0x84),
                        Color.FromArgb(0xFF, 0x66, 0xBB, 0x6A),
                        Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50),
                        Color.FromArgb(0xFF, 0x43, 0xA0, 0x47),
                        Color.FromArgb(0xFF, 0x38, 0x8E, 0x3C),
                        Color.FromArgb(0xFF, 0x2E, 0x7D, 0x32),
                        Color.FromArgb(0xFF, 0x1B, 0x5E, 0x20),
                    },

                    // Light Green
                    {
                        Color.FromArgb(0xFF, 0xF1, 0xF8, 0xE9),
                        Color.FromArgb(0xFF, 0xDC, 0xED, 0xC8),
                        Color.FromArgb(0xFF, 0xC5, 0xE1, 0xA5),
                        Color.FromArgb(0xFF, 0xAE, 0xD5, 0x81),
                        Color.FromArgb(0xFF, 0x9C, 0xCC, 0x65),
                        Color.FromArgb(0xFF, 0x8B, 0xC3, 0x4A),
                        Color.FromArgb(0xFF, 0x7C, 0xB3, 0x42),
                        Color.FromArgb(0xFF, 0x68, 0x9F, 0x38),
                        Color.FromArgb(0xFF, 0x55, 0x8B, 0x2F),
                        Color.FromArgb(0xFF, 0x33, 0x69, 0x1E),
                    },

                    // Lime
                    {
                        Color.FromArgb(0xFF, 0xF9, 0xFB, 0xE7),
                        Color.FromArgb(0xFF, 0xF0, 0xF4, 0xC3),
                        Color.FromArgb(0xFF, 0xE6, 0xEE, 0x9C),
                        Color.FromArgb(0xFF, 0xDC, 0xE7, 0x75),
                        Color.FromArgb(0xFF, 0xD4, 0xE1, 0x57),
                        Color.FromArgb(0xFF, 0xCD, 0xDC, 0x39),
                        Color.FromArgb(0xFF, 0xC0, 0xCA, 0x33),
                        Color.FromArgb(0xFF, 0xAF, 0xB4, 0x2B),
                        Color.FromArgb(0xFF, 0x9E, 0x9D, 0x24),
                        Color.FromArgb(0xFF, 0x82, 0x77, 0x17),
                    },

                    // Yellow
                    {
                        Color.FromArgb(0xFF, 0xFF, 0xFD, 0xE7),
                        Color.FromArgb(0xFF, 0xFF, 0xF9, 0xC4),
                        Color.FromArgb(0xFF, 0xFF, 0xF5, 0x9D),
                        Color.FromArgb(0xFF, 0xFF, 0xF1, 0x76),
                        Color.FromArgb(0xFF, 0xFF, 0xEE, 0x58),
                        Color.FromArgb(0xFF, 0xFF, 0xEB, 0x3B),
                        Color.FromArgb(0xFF, 0xFD, 0xD8, 0x35),
                        Color.FromArgb(0xFF, 0xFB, 0xC0, 0x2D),
                        Color.FromArgb(0xFF, 0xF9, 0xA8, 0x25),
                        Color.FromArgb(0xFF, 0xF5, 0x7F, 0x17),
                    },

                    // Amber
                    {
                        Color.FromArgb(0xFF, 0xFF, 0xF8, 0xE1),
                        Color.FromArgb(0xFF, 0xFF, 0xEC, 0xB3),
                        Color.FromArgb(0xFF, 0xFF, 0xE0, 0x82),
                        Color.FromArgb(0xFF, 0xFF, 0xD5, 0x4F),
                        Color.FromArgb(0xFF, 0xFF, 0xCA, 0x28),
                        Color.FromArgb(0xFF, 0xFF, 0xC1, 0x07),
                        Color.FromArgb(0xFF, 0xFF, 0xB3, 0x00),
                        Color.FromArgb(0xFF, 0xFF, 0xA0, 0x00),
                        Color.FromArgb(0xFF, 0xFF, 0x8F, 0x00),
                        Color.FromArgb(0xFF, 0xFF, 0x6F, 0x00),
                    },

                    // Orange
                    {
                        Color.FromArgb(0xFF, 0xFF, 0xF3, 0xE0),
                        Color.FromArgb(0xFF, 0xFF, 0xE0, 0xB2),
                        Color.FromArgb(0xFF, 0xFF, 0xCC, 0x80),
                        Color.FromArgb(0xFF, 0xFF, 0xB7, 0x4D),
                        Color.FromArgb(0xFF, 0xFF, 0xA7, 0x26),
                        Color.FromArgb(0xFF, 0xFF, 0x98, 0x00),
                        Color.FromArgb(0xFF, 0xFB, 0x8C, 0x00),
                        Color.FromArgb(0xFF, 0xF5, 0x7C, 0x00),
                        Color.FromArgb(0xFF, 0xEF, 0x6C, 0x00),
                        Color.FromArgb(0xFF, 0xE6, 0x51, 0x00),
                    },

                    // Deep Orange
                    {
                        Color.FromArgb(0xFF, 0xFB, 0xE9, 0xE7),
                        Color.FromArgb(0xFF, 0xFF, 0xCC, 0xBC),
                        Color.FromArgb(0xFF, 0xFF, 0xAB, 0x91),
                        Color.FromArgb(0xFF, 0xFF, 0x8A, 0x65),
                        Color.FromArgb(0xFF, 0xFF, 0x70, 0x43),
                        Color.FromArgb(0xFF, 0xFF, 0x57, 0x22),
                        Color.FromArgb(0xFF, 0xF4, 0x51, 0x1E),
                        Color.FromArgb(0xFF, 0xE6, 0x4A, 0x19),
                        Color.FromArgb(0xFF, 0xD8, 0x43, 0x15),
                        Color.FromArgb(0xFF, 0xBF, 0x36, 0x0C),
                    },

                    // Brown
                    {
                        Color.FromArgb(0xFF, 0xEF, 0xEB, 0xE9),
                        Color.FromArgb(0xFF, 0xD7, 0xCC, 0xC8),
                        Color.FromArgb(0xFF, 0xBC, 0xAA, 0xA4),
                        Color.FromArgb(0xFF, 0xA1, 0x88, 0x7F),
                        Color.FromArgb(0xFF, 0x8D, 0x6E, 0x63),
                        Color.FromArgb(0xFF, 0x79, 0x55, 0x48),
                        Color.FromArgb(0xFF, 0x6D, 0x4C, 0x41),
                        Color.FromArgb(0xFF, 0x5D, 0x40, 0x37),
                        Color.FromArgb(0xFF, 0x4E, 0x34, 0x2E),
                        Color.FromArgb(0xFF, 0x3E, 0x27, 0x23),
                    },

                    // Gray
                    {
                        Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA),
                        Color.FromArgb(0xFF, 0xF5, 0xF5, 0xF5),
                        Color.FromArgb(0xFF, 0xEE, 0xEE, 0xEE),
                        Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0),
                        Color.FromArgb(0xFF, 0xBD, 0xBD, 0xBD),
                        Color.FromArgb(0xFF, 0x9E, 0x9E, 0x9E),
                        Color.FromArgb(0xFF, 0x75, 0x75, 0x75),
                        Color.FromArgb(0xFF, 0x61, 0x61, 0x61),
                        Color.FromArgb(0xFF, 0x42, 0x42, 0x42),
                        Color.FromArgb(0xFF, 0x21, 0x21, 0x21),
                    },

                    // Blue Gray
                    {
                        Color.FromArgb(0xFF, 0xEC, 0xEF, 0xF1),
                        Color.FromArgb(0xFF, 0xCF, 0xD8, 0xDC),
                        Color.FromArgb(0xFF, 0xB0, 0xBE, 0xC5),
                        Color.FromArgb(0xFF, 0x90, 0xA4, 0xAE),
                        Color.FromArgb(0xFF, 0x78, 0x90, 0x9C),
                        Color.FromArgb(0xFF, 0x60, 0x7D, 0x8B),
                        Color.FromArgb(0xFF, 0x54, 0x6E, 0x7A),
                        Color.FromArgb(0xFF, 0x45, 0x5A, 0x64),
                        Color.FromArgb(0xFF, 0x37, 0x47, 0x4F),
                        Color.FromArgb(0xFF, 0x26, 0x32, 0x38),
                    },
                };

                _colorChartColorCount = _colorChart.GetLength(0);
                _colorChartShadeCount = _colorChart.GetLength(1);
            }

            return;
        }

        /// <inheritdoc/>
        public int ColorCount
        {
            // Table is transposed compared to the reference chart
            get
            {
                if (_colorChart == null)
                {
                    InitColorChart();
                }

                return _colorChartColorCount;
            }
        }

        /// <inheritdoc/>
        public int ShadeCount
        {
            // Table is transposed compared to the reference chart
            get
            {
                if (_colorChart == null)
                {
                    InitColorChart();
                }

                return _colorChartShadeCount;
            }
        }

        /// <inheritdoc/>
        public Color GetColor(int colorIndex, int shadeIndex)
        {
            if (_colorChart == null)
            {
                InitColorChart();
            }

            // Table is transposed compared to the reference chart
            return _colorChart![
                MathUtilities.Clamp(colorIndex, 0, _colorChartColorCount - 1),
                MathUtilities.Clamp(shadeIndex, 0, _colorChartShadeCount - 1)];
        }
    }
}
