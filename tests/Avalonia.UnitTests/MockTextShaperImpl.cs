using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockTextShaperImpl : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options)
        {
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;
            var shapedBuffer = new ShapedBuffer(text, text.Length, typeface, fontRenderingEmSize, bidiLevel);
            var targetInfos = shapedBuffer.GlyphInfos;
            var textSpan = text.Span;
            var textStartIndex = TextTestHelper.GetStartCharIndex(text);

            for (var i = 0; i < shapedBuffer.Length;)
            {
                var glyphCluster = i + textStartIndex;

                var codepoint = Codepoint.ReadAt(textSpan, i, out var count);

                var glyphIndex = typeface.GetGlyph(codepoint);

                for (var j = 0; j < count; ++j)
                {
                    targetInfos[i + j] = new GlyphInfo(glyphIndex, glyphCluster, 10);
                }

                i += count;
            }

            return shapedBuffer;
        }
    }
}
