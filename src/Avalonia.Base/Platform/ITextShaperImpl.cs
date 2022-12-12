using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// An abstraction that is used produce shaped text.
    /// </summary>
    [Unstable]
    public interface ITextShaperImpl
    {
        /// <summary>
        /// Shapes the specified region within the text and returns a shaped buffer.
        /// </summary>
        /// <param name="text">The text buffer.</param>
        /// <param name="length">length of text</param>
        /// <param name="options">Text shaper options to customize the shaping process.</param>
        /// <returns>A shaped glyph run.</returns>
        ShapedBuffer ShapeText(CharacterBufferReference text, int length, TextShaperOptions options);
    }   
}
