using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.UnitTests
{
    public class MockTextShaperImpl : ITextShaperImpl
    {
        public GlyphRun ShapeText(ReadOnlySlice<char> text, Typeface typeface, double fontRenderingEmSize, CultureInfo culture)
        {
            var glyphTypeface = typeface.GlyphTypeface;
            var glyphIndices = new ushort[text.Length];
            var glyphCount = 0;

            for (var i = 0; i < text.Length;)
            {
                var index = i;

                var codepoint = Codepoint.ReadAt(text, i, out var count);

                i += count;

                var glyph = glyphTypeface.GetGlyph(codepoint);

                glyphIndices[index] = glyph;

                glyphCount++;
            }

            return new GlyphRun(glyphTypeface, fontRenderingEmSize,
                new ReadOnlySlice<ushort>(glyphIndices.AsMemory(0, glyphCount)), characters: text);
        }
    }
}
