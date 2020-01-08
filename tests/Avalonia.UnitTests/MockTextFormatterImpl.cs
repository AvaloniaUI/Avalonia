using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Media.Text.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.UnitTests
{
    public class MockTextFormatterImpl : ITextFormatterImpl
    {
        public List<TextRunProperties> CreateTextRuns(ReadOnlySlice<char> text, TextRunStyle defaultStyle)
        {
            return new List<TextRunProperties>
            {
                new TextRunProperties(text.GetTextPointer(), defaultStyle)
            };
        }

        public GlyphRun CreateShapedGlyphRun(ReadOnlySlice<char> text, TextFormat textFormat)
        {
            var glyphTypeface = textFormat.Typeface.GlyphTypeface;
            var glyphIndices = new ushort[text.Length];

            for (var i = 0; i < text.Length;)
            {
                var index = i;

                var codepoint = CodepointReader.Read(text, ref i);

                var glyph = glyphTypeface.GetGlyph((uint)codepoint);

                glyphIndices[index] = glyph;
            }

            return new GlyphRun(glyphTypeface, textFormat.FontRenderingEmSize, glyphIndices);
        }
    }
}
