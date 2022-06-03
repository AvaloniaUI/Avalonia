using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Interface to define a color palette.
    /// </summary>
    public interface IColorPalette
    {
        /// <summary>
        /// Gets the total number of colors in this palette.
        /// A color is not necessarily a single value and may be composed of several shades.
        /// </summary>
        /// <remarks>
        /// Represents total columns in a table.
        /// </remarks>
        int ColorCount { get; }

        /// <summary>
        /// Gets the total number of shades for each color in this palette.
        /// Shades are usually a variation of the color lightening or darkening it.
        /// </summary>
        /// <remarks>
        /// Represents total rows in a table.
        /// </remarks>
        int ShadeCount { get; }

        /// <summary>
        /// Gets a color in the palette by index.
        /// </summary>
        /// <param name="colorIndex">The index of the color in the palette.
        /// The index must be between zero and <see cref="ColorCount"/>.</param>
        /// <param name="shadeIndex">The index of the color shade in the palette.
        /// The index must be between zero and <see cref="ShadeCount"/>.</param>
        /// <returns>The color at the specified index or an exception.</returns>
        Color GetColor(int colorIndex, int shadeIndex);
    }
}
