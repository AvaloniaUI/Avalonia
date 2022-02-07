using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Platform
{
    /// <summary>
    /// An abstraction that is used produce shaped text.
    /// </summary>
    public interface ITextShaperImpl
    {
        /// <summary>
        /// Shapes the specified region within the text and returns a shaped buffer.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontRenderingEmSize">The font rendering em size.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="bidiLevel">The bidi level.</param>
        /// <returns>A shaped glyph run.</returns>
        ShapedBuffer ShapeText(ReadOnlySlice<char> text, GlyphTypeface typeface, double fontRenderingEmSize, CultureInfo? culture, sbyte bidiLevel);
    }
}
