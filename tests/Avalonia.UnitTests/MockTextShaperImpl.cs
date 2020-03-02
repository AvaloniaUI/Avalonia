using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.UnitTests
{
    public class MockTextShaperImpl : ITextShaperImpl
    {
        public GlyphRun ShapeText(ReadOnlySlice<char> text, TextFormat textFormat)
        {
            var glyphTypeface = textFormat.Typeface.GlyphTypeface;
            var glyphIndices = new ushort[text.Length];
            var height = textFormat.FontMetrics.LineHeight;
            var width = 0.0;

            for (var i = 0; i < text.Length;)
            {
                var index = i;

                var codepoint = Codepoint.ReadAt(text, i, out var count);

                i += count;

                var glyph = glyphTypeface.GetGlyph(codepoint);

                glyphIndices[index] = glyph;

                width += glyphTypeface.GetGlyphAdvance(glyph);
            }

            return new GlyphRun(glyphTypeface, textFormat.FontRenderingEmSize, glyphIndices, characters: text,
                bounds: new Rect(0, 0, width, height));
        }
    }
}
