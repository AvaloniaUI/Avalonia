using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using SharpDX.DirectWrite;

#nullable enable

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        private readonly DWriteTypeface _glyphTypefaceImpl;

        private readonly short[] _glyphIndices;
        private readonly float[] _glyphAdvances;
        private readonly GlyphOffset[] _glyphOffsets;

        private SharpDX.DirectWrite.GlyphRun? _glyphRun;

        public GlyphRunImpl(IGlyphTypeface glyphTypeface, double fontRenderingEmSize,
            IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin)
        {
            _glyphTypefaceImpl = (DWriteTypeface)glyphTypeface.PlatformTypeface;

            FontRenderingEmSize = fontRenderingEmSize;
            BaselineOrigin = baselineOrigin;

            var glyphCount = glyphInfos.Count;

            _glyphIndices = new short[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                _glyphIndices[i] = (short)glyphInfos[i].GlyphIndex;
            }

            _glyphAdvances = new float[glyphCount];

            var width = 0.0;

            for (var i = 0; i < glyphCount; i++)
            {
                var advance = glyphInfos[i].GlyphAdvance;

                width += advance;

                _glyphAdvances[i] = (float)advance;
            }

            _glyphOffsets = new GlyphOffset[glyphCount];
            
            var runBounds = new Rect();
            var currentX = 0.0;
            var scale = fontRenderingEmSize / glyphTypeface.Metrics.DesignEmHeight;
            for (var i = 0; i < glyphCount; i++)
            {
                var (x, y) = glyphInfos[i].GlyphOffset;

                _glyphOffsets[i] = new GlyphOffset
                {
                    AdvanceOffset = (float)x,
                    AscenderOffset = (float)y
                };

                if (glyphTypeface.TryGetGlyphMetrics(glyphInfos[i].GlyphIndex, out var metrics))
                {
                    // Found metrics with negative height, prefer to adjust it to positive.
                    var ybearing = metrics.YBearing;
                    var height = metrics.Height;
                    if (height < 0)
                    {
                        height = (ushort)-height;
                    }

                    // Not entirely sure about why we need to do this, but it seems to work
                    var xOffset = metrics.XBearing * scale;
                    var xWidth = xOffset > 0 ? xOffset : 0;
                    var xBearing = xOffset < 0 ? xOffset : 0;

                    //yBearing is the vertical distance from the baseline to the top of the glyph's bbox. It is usually positive for horizontal layouts, and negative for vertical ones.
                    runBounds = runBounds.Union(new Rect(currentX + xBearing, baselineOrigin.Y - ybearing * scale, xWidth + metrics.Width * scale, height * scale));
                }

                currentX += glyphInfos[i].GlyphAdvance;
            }

            Bounds = runBounds.Translate(new Vector(baselineOrigin.X, 0));
        }

        public SharpDX.DirectWrite.GlyphRun GlyphRun
        {
            get
            {
                if (_glyphRun != null)
                {
                    return _glyphRun;
                }

                _glyphRun = new SharpDX.DirectWrite.GlyphRun
                {
                    FontFace = _glyphTypefaceImpl.FontFace,
                    FontSize = (float)FontRenderingEmSize,
                    Advances = _glyphAdvances,
                    Indices = _glyphIndices,
                    Offsets = _glyphOffsets
                };

                return _glyphRun;
            }
        }

        public double FontRenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public Rect Bounds { get; }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound) => Array.Empty<float>();

        public void Dispose()
        {
            //_glyphRun?.Dispose();

            _glyphRun = null;
        }
    }
}
