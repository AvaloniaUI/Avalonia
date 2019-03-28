// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

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

        public short[] GetGlyphs(ReadOnlySpan<int> codePoints) => GlyphTypefaceImpl.GetGlyphs(codePoints);

        public short[] GetGlyphs(int[] codePoints) => GlyphTypefaceImpl.GetGlyphs(codePoints);

        public ReadOnlySpan<int> GetGlyphAdvances(short[] glyphs) => GlyphTypefaceImpl.GetGlyphAdvances(glyphs);

        private IGlyphTypefaceImpl CreateGlyphTypefaceImpl()
        {
            return AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateGlyphTypeface(_typeface);
        }
    }
}
