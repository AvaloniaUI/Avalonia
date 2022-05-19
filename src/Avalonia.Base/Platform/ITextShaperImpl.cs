using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Utilities;

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
        /// <param name="text">The text.</param>
        /// <param name="options">Text shaper options to customize the shaping process.</param>
        /// <returns>A shaped glyph run.</returns>
        ShapedBuffer ShapeText(ReadOnlySlice<char> text, TextShaperOptions options);
    }   
}
