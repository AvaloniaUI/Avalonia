using System;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockGlyphTypeface : IGlyphTypefaceImpl
    {
        public short DesignEmHeight => 10;
        public int Ascent => 2;
        public int Descent => 10;
        public int LineGap { get; }
        public int UnderlinePosition { get; }
        public int UnderlineThickness { get; }
        public int StrikethroughPosition { get; }
        public int StrikethroughThickness { get; }
        public bool IsFixedPitch { get; }
        public int GlyphCount => 1337;

        public ushort GetGlyph(uint codepoint)
        {
            return (ushort)codepoint;
        }

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            return new ushort[codepoints.Length];
        }

        public int GetGlyphAdvance(ushort glyph)
        {
            return 8;
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var advances = new int[glyphs.Length];

            for (var i = 0; i < advances.Length; i++)
            {
                advances[i] = 8;
            }

            return advances;
        }

        public void Dispose() { }
    }
}
