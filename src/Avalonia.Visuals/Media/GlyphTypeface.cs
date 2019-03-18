// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

using Avalonia.Platform;

namespace Avalonia.Media
{  
    public class GlyphTypeface : IDisposable
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

        public double Ascent => GlyphTypefaceImpl.Ascent;

        public double Descent => GlyphTypefaceImpl.Descent;

        public double Leading => GlyphTypefaceImpl.Leading;

        public double UnderlinePosition => GlyphTypefaceImpl.UnderlinePosition;

        public double UnderlineThickness => GlyphTypefaceImpl.UnderlineThickness;

        public double StrikethroughPosition => GlyphTypefaceImpl.StrikethroughPosition;

        public double StrikethroughThickness => GlyphTypefaceImpl.StrikethroughThickness;

        public ushort CharacterToGlyph(char c) => GlyphTypefaceImpl.CharacterToGlyph(c);

        public ushort CharacterToGlyph(int c) => GlyphTypefaceImpl.CharacterToGlyph(c);

        public double GetHorizontalGlyphAdvance(ushort glyph) => GlyphTypefaceImpl.GetHorizontalGlyphAdvance(glyph);

        public IReadOnlyList<ushort> CharactersToGlyphs(string s)
        {            
            return CharactersToGlyphs(s.AsSpan());
        }

        public IReadOnlyList<ushort> CharactersToGlyphs(ReadOnlySpan<char> characters)
        {
            var glyphs = new ushort[characters.Length];

            for (var i = 0; i < characters.Length; i++)
            {
                glyphs[i] = CharacterToGlyph(characters[i]);
            }

            return glyphs;
        }

        public IReadOnlyList<ushort> CharactersToGlyphs(ReadOnlySpan<int> characters)
        {
            var glyphs = new ushort[characters.Length];

            for (var i = 0; i < characters.Length; i++)
            {
                glyphs[i] = CharacterToGlyph(characters[i]);
            }

            return glyphs;
        }

        public void Dispose()
        {
            GlyphTypefaceImpl.Dispose();
        }

        private IGlyphTypefaceImpl CreateGlyphTypefaceImpl()
        {
            return AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateGlyphTypeface(_typeface);
        }
    }
}
