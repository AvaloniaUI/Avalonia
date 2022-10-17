using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements the standard sixteen color palette from the HTML 4.01 specification.
    /// </summary>
    /// <remarks>
    /// See https://en.wikipedia.org/wiki/Web_colors#HTML_color_names.
    /// </remarks>
    public class SixteenColorPalette : IColorPalette
    {
        // The 16 standard colors from HTML and early Windows computers
        // https://en.wikipedia.org/wiki/List_of_software_palettes
        // https://en.wikipedia.org/wiki/Web_colors#HTML_color_names
        private static Color[,] colorChart = new Color[,]
        {
            {
                Colors.White,
                Colors.Silver
            },
            {
                Colors.Gray,
                Colors.Black
            },
            {
                Colors.Red,
                Colors.Maroon
            },
            {
                Colors.Yellow,
                Colors.Olive
            },
            {
                Colors.Lime,
                Colors.Green
            },
            {
                Colors.Aqua,
                Colors.Teal
            },
            {
                Colors.Blue,
                Colors.Navy
            },
            {
                Colors.Fuchsia,
                Colors.Purple
            }
        };

        /// <inheritdoc/>
        public int ColorCount
        {
            get => colorChart.GetLength(0);
        }

        /// <inheritdoc/>
        public int ShadeCount
        {
            get => colorChart.GetLength(1);
        }

        /// <inheritdoc/>
        public Color GetColor(int colorIndex, int shadeIndex)
        {
            return colorChart[
                MathUtilities.Clamp(colorIndex, 0, colorChart.GetLength(0) - 1),
                MathUtilities.Clamp(shadeIndex, 0, colorChart.GetLength(1) - 1)];
        }
    }
}
