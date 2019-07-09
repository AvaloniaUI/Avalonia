// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
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
            _tableLoader = new TableLoader(typeface);

            Font.GetScale(out var xScale, out _);

            DesignEmHeight = (short)xScale;

            var horizontalFontExtents = Font.HorizontalFontExtents;

            Ascent = -horizontalFontExtents.Ascender;

            Descent = -horizontalFontExtents.Descender;

            LineGap = horizontalFontExtents.LineGap;
        }

        public SKTypeface Typeface => _tableLoader.Typeface;

        public Font Font => _tableLoader.Font;

        public short DesignEmHeight { get; }

        public int Ascent { get; }

        public int Descent { get; }

        public int LineGap { get; }

        public int UnderlinePosition => 0;

        public int UnderlineThickness => 0;

        public int StrikethroughPosition => 0;

        public int StrikethroughThickness => 0;

        public ushort[] GetGlyphs(ReadOnlySpan<int> codePoints)
        {
            var glyphs = new ushort[codePoints.Length];

            for (var i = 0; i < codePoints.Length; i++)
            {
                glyphs[i] = (ushort)Font.GetGlyph(codePoints[i]);
            }

            return glyphs;
        }

        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var glyphIndices = new int[glyphs.Length];

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

        public void Dispose()
        {
            _tableLoader.Dispose();
        }

        private class TableLoader : IDisposable
        {

            private readonly Dictionary<Tag, Blob> _tableCache = new Dictionary<Tag, Blob>();
            private bool _isDisposed;

            public TableLoader(SKTypeface typeface)
            {
                Typeface = typeface;
                Font = CreateFont(typeface);
            }

            public SKTypeface Typeface { get; }


            public Font Font { get; }

            private Font CreateFont(SKTypeface typeface)
            {
                var face = new Face(GetTable, Dispose)
                {
                    UnitsPerEm = typeface.UnitsPerEm
                };

                var font = new Font(face);

                font.SetFunctionsOpenType();

                return font;
            }

            private void Dispose(bool disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;

                if (!disposing)
                {
                    return;
                }

                foreach (var blob in _tableCache.Values)
                {
                    blob?.Dispose();
                }

                Font.Dispose();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private unsafe Blob CreateBlob(Tag tag)
            {
                if (Typeface.TryGetTableData(tag, out var table))
                {
                    fixed (byte* tablePtr = table)
                    {
                        return new Blob((IntPtr)tablePtr, table.Length, MemoryMode.Duplicate);
                    }
                }

                return null;
            }

            private IntPtr GetTable(IntPtr face, Tag tag, IntPtr userData)
            {
                Blob blob;

                if (_tableCache.ContainsKey(tag))
                {
                    blob = _tableCache[tag];
                }
                else
                {
                    blob = CreateBlob(tag);
                    _tableCache.Add(tag, blob);
                }

                return blob?.Handle ?? IntPtr.Zero;
            }
        }
    }
}
