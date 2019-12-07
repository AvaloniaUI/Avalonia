using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Media.Text.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.UnitTests
{
    public class MockTextFormatter : ITextFormatter
    {
        public List<TextRunProperties> CreateTextRuns(ReadOnlySlice<char> text, Typeface defaultTypeface, double defaultFontSize)
        {
            return new List<TextRunProperties>
            {
                new TextRunProperties(text, defaultTypeface, 12, null)
            };
        }

        public List<TextRun> FormatTextRuns(ReadOnlySlice<char> text, List<TextRunProperties> textRunProperties)
        {
            var textRuns = new List<TextRun>();

            foreach (var runProperties in textRunProperties)
            {
                var glyphRun = CreateShapedGlyphRun(runProperties.Text, runProperties.TextFormat);

                textRuns.Add(new TextRun(glyphRun, runProperties.TextFormat, runProperties.Foreground));
            }

            return textRuns;
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

            return new GlyphRun(glyphTypeface, textFormat.FontSize, glyphIndices);
        }
    }
}
