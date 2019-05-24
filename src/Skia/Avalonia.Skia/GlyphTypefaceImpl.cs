// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using HarfBuzzSharp;

using SkiaSharp;

namespace Avalonia.Skia
{
    // ToDo: Use this for the TextLayout
    internal class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        public GlyphTypefaceImpl(SKTypeface typeface)
        {
            Typeface = typeface;

            Font = CreateHarfBuzzFont(typeface);

            Font.GetScale(out var xScale, out _);

            DesignEmHeight = (short)xScale;

            var horizontalFontExtents = Font.HorizontalFontExtents;

            Ascent = -horizontalFontExtents.Ascender;

            Descent = -horizontalFontExtents.Descender;

            LineGap = horizontalFontExtents.LineGap;
        }

        /// <summary>
        /// Creates a <see cref="Font"/> instance from specified <see cref="SKTypeface"/>
        /// </summary>
        /// <param name="typeface"></param>
        /// <returns></returns>
        private static Font CreateHarfBuzzFont(SKTypeface typeface)
        {
            var face = new Face(new TypefaceTableLoader(typeface))
            {
                UnitsPerEm = typeface.UnitsPerEm
            };

            var font = new Font(face);

            font.SetFunctionsOpenType();

            return font;
        }

        public SKTypeface Typeface { get; }

        public Font Font { get; }

        public short DesignEmHeight { get; }

        public int Ascent { get; }

        public int Descent { get; }

        public int LineGap { get; }

        public int UnderlinePosition => 0;

        public int UnderlineThickness => 0;

        public int StrikethroughPosition => 0;

        public int StrikethroughThickness => 0;

        public short[] GetGlyphs(ReadOnlySpan<int> codePoints)
        {
            var glyphs = new short[codePoints.Length];

            for (var i = 0; i < codePoints.Length; i++)
            {
                glyphs[i] = (short)Font.GetGlyph(codePoints[i]);
            }

            return glyphs;
        }

        public short[] GetGlyphs(int[] codePoints)
        {
            var glyphs = new short[codePoints.Length];

            for (var i = 0; i < codePoints.Length; i++)
            {
                glyphs[i] = (short)Font.GetGlyph(codePoints[i]);
            }

            return glyphs;
        }

        public ReadOnlySpan<int> GetGlyphAdvances(short[] glyphs)
        {
            var indices = new int[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                indices[i] = glyphs[i];
            }

            return Font.GetHorizontalGlyphAdvances(indices);
        }

        public void Dispose()
        {
            Font.Dispose();
        }

        private class TypefaceTableLoader : TableLoader
        {
            private readonly SKTypeface _typeface;

            public TypefaceTableLoader(SKTypeface typeface)
            {
                _typeface = typeface;
            }

            /// <summary>
            /// Loads the requested table for use within HarfBuzz
            /// </summary>
            /// <param name="tag"></param>
            /// <returns></returns>
            protected override unsafe Blob Load(Tag tag)
            {
                if (_typeface.TryGetTableData(tag, out var table))
                {
                    fixed (byte* tablePtr = table)
                    {
                        // This needs to copy the array on creation (MemoryMode.Duplicate)
                        return new Blob((IntPtr)tablePtr, table.Length, MemoryMode.Duplicate);
                    }
                }

                return null;
            }
        }
    }
}
