// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Skia.Text;
using HarfBuzzSharp;

using SkiaSharp;

namespace Avalonia.Skia
{
    // ToDo: Use this for the TextLayout
    internal class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        private readonly TableLoader _tableLoader;

        public GlyphTypefaceImpl(SKTypeface typeface)
        {
            Typeface = typeface;

            _tableLoader = new TableLoader(Typeface);

            Font.GetScale(out var xScale, out _);

            DesignEmHeight = (short)xScale;

            if (!Font.TryGetHorizontalFontExtents(out var fontExtents))
            {
                return;
            }

            Ascent = -fontExtents.Ascender;

            Descent = -fontExtents.Descender;

            LineGap = fontExtents.LineGap;
        }

        private Font Font => _tableLoader.Font;

        public SKTypeface Typeface { get; }

        public short DesignEmHeight { get; }

        public int Ascent { get; }

        public int Descent { get; }

        public int LineGap { get; }

        public int UnderlinePosition => 0;

        public int UnderlineThickness => 0;

        public int StrikethroughPosition => 0;

        public int StrikethroughThickness => 0;

        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            var glyphs = new ushort[codepoints.Length];

            for (var i = 0; i < codepoints.Length; i++)
            {
                if (Font.TryGetGlyph(codepoints[i], out var glyph))
                {
                    glyphs[i] = (ushort)glyph;
                }
            }

            return glyphs;
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var glyphIndices = new uint[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphIndices[i] = glyphs[i];
            }

            return Font.GetHorizontalGlyphAdvances(glyphIndices);
        }

        public IGlyphRunImpl CreateGlyphRun(float fontRenderingEmSize, Point baselineOrigin, IReadOnlyList<ushort> glyphIndices,
            IReadOnlyList<float> glyphAdvances, IReadOnlyList<Vector> glyphOffsets)
        {
            var scale = fontRenderingEmSize / DesignEmHeight;
            var currentX = 0.0f;

            if (glyphOffsets == null)
            {
                glyphOffsets = new Vector[glyphIndices.Count];
            }

            if (glyphAdvances == null)
            {
                var advances = new float[glyphIndices.Count];

                for (var i = 0; i < glyphIndices.Count; i++)
                {
                    advances[i] = Font.GetHorizontalGlyphAdvance(glyphIndices[i]) * scale;
                }

                glyphAdvances = advances;
            }

            var glyphPositions = new SKPoint[glyphIndices.Count];

            for (var i = 0; i < glyphIndices.Count; i++)
            {
                var glyphOffset = glyphOffsets[i];

                glyphPositions[i] = new SKPoint(currentX + (float)glyphOffset.X, (float)glyphOffset.Y);

                currentX += glyphAdvances[i];
            }

            var bounds = new Rect(baselineOrigin.X, baselineOrigin.Y + Ascent * scale, currentX, (Descent - Ascent + LineGap) * scale);

            return new GlyphRunImpl(glyphPositions, bounds);
        }
    }
}
