using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class FontManagerStub : IFontManagerImpl
    {
        public string GetDefaultFontFamilyName()
        {
            return "Droid Sans";
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            return new[] { "Droid Sans" };
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontFamily fontFamily,
            CultureInfo culture, out Typeface typeface)
        {
            typeface = Typeface.Default;
            return true;
        }

        class GlyphTypefaceImpl : IGlyphTypefaceImpl
        {
            public void Dispose()
            {

            }

            public ushort GetGlyph(uint codepoint)
            {
                return 10;
            }

            public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
            {
                return Enumerable.Repeat((ushort)10, codepoints.Length).ToArray();
            }

            public int GetGlyphAdvance(ushort glyph)
            {
                return 0;
            }

            public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
            {
                return Enumerable.Repeat(0, glyphs.Length).ToArray();
            }

            public short DesignEmHeight => 10;
            public int Ascent => 10;
            public int Descent => 10;
            public int LineGap => 10;
            public int UnderlinePosition => 10;
            public int UnderlineThickness => 10;
            public int StrikethroughPosition => 10;
            public int StrikethroughThickness => 10;
            public bool IsFixedPitch => true;
        }

        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface) => new GlyphTypefaceImpl();
    }


}