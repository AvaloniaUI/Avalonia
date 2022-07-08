using System;
using Avalonia.Platform;
using Avalonia.Native.Interop;
namespace Avalonia.NativeGraphics.Backend
{
    public class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private IAvgGlyphTypeface _native;

        public GlyphTypefaceImpl(IAvgGlyphTypeface avgGlyphTypeface)
        {
            _native = avgGlyphTypeface;
        }

        public IAvgGlyphTypeface Typeface => _native;
        
        public void Dispose()
        {

        }

        public ushort GetGlyph(uint codepoint)
        {
            return (ushort) _native.GetGlyph(codepoint);
        }

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            ushort[] glyphsArr = new ushort[codepoints.Length];
            int i = 0;
            foreach (var codepoint in codepoints)
            {
                glyphsArr[i++] = GetGlyph(codepoint);
            }

            return glyphsArr;
        }

        public int GetGlyphAdvance(ushort glyph)
        {
            return _native.GetGlyphAdvance(glyph);
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            int[] advancesArr = new int[glyphs.Length];
            int i = 0;
            foreach (var glyph in glyphs)
            {
                advancesArr[i++] = GetGlyphAdvance(glyph);
            }

            return advancesArr;
        }

        public short DesignEmHeight => (short) _native.DesignEmHeight;
        public int Ascent => _native.Ascent;
        public int Descent => _native.Descent;
        public int LineGap => _native.LineGap;
        public int UnderlinePosition => _native.UnderlinePosition;
        public int UnderlineThickness => _native.UnderlineThickness;
        public int StrikethroughPosition => _native.StrikethroughPosition;
        public int StrikethroughThickness => _native.StrikethroughThickness;
        public bool IsFixedPitch => Convert.ToBoolean(_native.IsFixedPitch);
    }
}