using System;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockGlyphTypeface : IGlyphTypefaceImpl
    {
        public short DesignEmHeight => 10;
        public int Ascent => 100;
        public int Descent => 0;
        public int LineGap { get; }
        public int UnderlinePosition { get; }
        public int UnderlineThickness { get; }
        public int StrikethroughPosition { get; }
        public int StrikethroughThickness { get; }
        public bool IsFixedPitch { get; }

        public ushort GetGlyph(uint codepoint)
        {
            return 0;
        }

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            return new ushort[codepoints.Length];
        }

        public int GetGlyphAdvance(ushort glyph)
        {
            return 100;
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var advances = new int[glyphs.Length];

            for (var i = 0; i < advances.Length; i++)
            {
                advances[i] = 100;
            }

            return advances;
        }

        public void Dispose() { }
    }
}
