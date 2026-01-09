using System;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// An abstraction that is used produce shaped text.
    /// </summary>
    [NotClientImplementable]
    public interface ITextShaperImpl
    {
        /// <summary>
        /// Shapes the specified region within the text and returns a shaped buffer.
        /// </summary>
        /// <param name="text">The text buffer.</param>
        /// <param name="options">Text shaper options to customize the shaping process.</param>
        /// <returns>A shaped glyph run.</returns>
        ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options);

        /// <summary>
        /// Creates a text shaper typeface based on the specified glyph typeface.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface to use as the basis for the text shaper typeface.</param>
        /// <returns>An instance of <see cref="ITextShaperTypeface"/> that represents the text shaping functionality for the
        /// specified glyph typeface.</returns>
        ITextShaperTypeface CreateTypeface(GlyphTypeface glyphTypeface);
    }
}
