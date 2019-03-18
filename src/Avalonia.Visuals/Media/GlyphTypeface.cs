// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media
{
    using System;
    using System.Collections.Generic;

    using Avalonia.Platform;

    public class GlyphTypeface
    {
        private readonly Typeface _typeface;

        private readonly Lazy<IGlyphTypefaceImpl> _glyphTypefaceImpl;

        public GlyphTypeface(Typeface typeface)
        {
            _typeface = typeface;
            _glyphTypefaceImpl = new Lazy<IGlyphTypefaceImpl>(CreateGlyphTypefaceImpl);
        }

        internal IGlyphTypefaceImpl GlyphTypefaceImpl => _glyphTypefaceImpl.Value;
        public FontStyle Style => _typeface.Style;
        public FontWeight Weight => _typeface.Weight;
        public int GlyphCount => GlyphTypefaceImpl.GlyphCount;

        private IGlyphTypefaceImpl CreateGlyphTypefaceImpl()
        {
            return AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateGlyphTypeface(_typeface);
        }
    }

    public interface IGlyphTypefaceImpl
    {
        int GlyphCount { get; }
        IDictionary<int, ushort> CharacterToGlyphMap { get; }
        double Baseline { get; }
        double UnderlinePosition { get; }
        double UnderlineThickness { get; }
        double StrikethroughPosition { get; }
        double StrikethroughThickness { get; }
    }
}
