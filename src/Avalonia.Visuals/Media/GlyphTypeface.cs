// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Text;

using Avalonia.Platform;

namespace Avalonia.Media
{
    public class GlyphTypeface
    {
        private readonly Typeface _typeface;

        private readonly Lazy<IGlyphTypefaceImpl> _glyphTypefaceImpl;

        public GlyphTypeface(Typeface typeface)
        {
            _typeface = typeface;
            _glyphTypefaceImpl = new Lazy<IGlyphTypefaceImpl>(CreateGlyphTypefaceImpl);
        }

        public IGlyphTypefaceImpl GlyphTypefaceImpl => _glyphTypefaceImpl.Value;

        public FontStyle Style => _typeface.Style;

        public FontWeight Weight => _typeface.Weight;

        public short DesignEmHeight => GlyphTypefaceImpl.DesignEmHeight;

        public int Ascent => GlyphTypefaceImpl.Ascent;

        public int Descent => GlyphTypefaceImpl.Descent;

        public int LineGap => GlyphTypefaceImpl.LineGap;

        public int UnderlinePosition => GlyphTypefaceImpl.UnderlinePosition;

        public int UnderlineThickness => GlyphTypefaceImpl.UnderlineThickness;

        public int StrikethroughPosition => GlyphTypefaceImpl.StrikethroughPosition;

        public int StrikethroughThickness => GlyphTypefaceImpl.StrikethroughThickness;

        public ReadOnlySpan<short> GetGlyphs(string text)
        {
            var bytes = Encoding.UTF32.GetBytes(text);

            var codepoints = new int[bytes.Length / 4];

            Buffer.BlockCopy(bytes, 0, codepoints, 0, bytes.Length);

            return GetGlyphs(codepoints);
        }

        public ReadOnlySpan<short> GetGlyphs(ReadOnlySpan<int> text) => GlyphTypefaceImpl.GetGlyphs(text);

        public ReadOnlySpan<int> GetGlyphAdvances(ReadOnlySpan<short> glyphs) => GlyphTypefaceImpl.GetGlyphAdvances(glyphs);

        private IGlyphTypefaceImpl CreateGlyphTypefaceImpl()
        {
            return AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateGlyphTypeface(_typeface);
        }
    }
}
