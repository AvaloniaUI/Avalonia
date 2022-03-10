using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.UnitTests
{
    public class MockTextShaperImpl : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlySlice<char> text, GlyphTypeface typeface, double fontRenderingEmSize,
            CultureInfo culture, sbyte bidiLevel)
        {
            var shapedBuffer = new ShapedBuffer(text, text.Length, typeface, fontRenderingEmSize, bidiLevel);

            for (var i = 0; i < shapedBuffer.Length;)
            {
                var glyphCluster = i + text.Start;
                var codepoint = Codepoint.ReadAt(text, i, out var count);

                var glyphIndex = typeface.GetGlyph(codepoint);

                shapedBuffer[i] = new GlyphInfo(glyphIndex, glyphCluster, 10);

                i += count;
            }

            return shapedBuffer;
        }
    }
}
