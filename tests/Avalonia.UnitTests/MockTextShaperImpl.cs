using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockTextShaperImpl : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(CharacterBufferReference text, int length, TextShaperOptions options)
        {
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;
            var characterBufferRange = new CharacterBufferRange(text, length);
            var shapedBuffer = new ShapedBuffer(characterBufferRange, length, typeface, fontRenderingEmSize, bidiLevel);

            for (var i = 0; i < shapedBuffer.Length;)
            {
                var glyphCluster = i + text.OffsetToFirstChar;

                var codepoint = Codepoint.ReadAt(characterBufferRange, i, out var count);

                var glyphIndex = typeface.GetGlyph(codepoint);

                shapedBuffer[i] = new GlyphInfo(glyphIndex, glyphCluster, 10);

                i += count;
            }

            return shapedBuffer;
        }
    }
}
