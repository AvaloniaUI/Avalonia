using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        private readonly GlyphTypefaceImpl _glyphTypefaceImpl;
        private readonly ushort[] _glyphIndices;
        private readonly SKPoint[] _glyphPositions;

        private readonly ConcurrentDictionary<SKFontEdging, SKTextBlob> _textBlobCache = new();

        public GlyphRunImpl(IGlyphTypeface glyphTypeface, double fontRenderingEmSize,
            IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin)
        {
            if (glyphTypeface == null)
            {
                throw new ArgumentNullException(nameof(glyphTypeface));
            }

            _glyphTypefaceImpl = (GlyphTypefaceImpl)glyphTypeface;

            if (glyphInfos == null)
            {
                throw new ArgumentNullException(nameof(glyphInfos));
            }

            var count = glyphInfos.Count;
            _glyphIndices = new ushort[count];
            _glyphPositions = new SKPoint[count];

            var currentX = 0.0;

            for (int i = 0; i < count; i++)
            {
                var glyphInfo = glyphInfos[i];
                var offset = glyphInfo.GlyphOffset;

                _glyphIndices[i] = glyphInfo.GlyphIndex;

                _glyphPositions[i] = new SKPoint((float)(currentX + offset.X), (float)offset.Y);

                currentX += glyphInfos[i].GlyphAdvance;
            }

            _glyphTypefaceImpl.SKFont.Size = (float)fontRenderingEmSize;

            var runBounds = new Rect();
            var glyphBounds = ArrayPool<SKRect>.Shared.Rent(glyphInfos.Count);

            _glyphTypefaceImpl.SKFont.GetGlyphWidths(_glyphIndices, null, glyphBounds);

            currentX = 0;

            for (var i = 0; i < glyphInfos.Count; i++)
            {
                var gBounds = glyphBounds[i];
                var advance = glyphInfos[i].GlyphAdvance;

                runBounds = runBounds.Union(new Rect(currentX + gBounds.Left, baselineOrigin.Y + gBounds.Top, gBounds.Width, gBounds.Height));

                currentX += advance;
            }

            ArrayPool<SKRect>.Shared.Return(glyphBounds);

            FontRenderingEmSize = fontRenderingEmSize;
            BaselineOrigin = baselineOrigin;
            Bounds = runBounds;
        }

        public IGlyphTypeface GlyphTypeface => _glyphTypefaceImpl;

        public double FontRenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public Rect Bounds { get; }

        public SKTextBlob GetTextBlob(RenderOptions renderOptions)
        {
            var edging = SKFontEdging.SubpixelAntialias;

            switch (renderOptions.TextRenderingMode)
            {
                case TextRenderingMode.Alias:
                    edging = SKFontEdging.Alias;
                    break;
                case TextRenderingMode.Antialias:
                    edging = SKFontEdging.Antialias;
                    break;
                case TextRenderingMode.Unspecified:
                    edging = renderOptions.EdgeMode == EdgeMode.Aliased ? SKFontEdging.Alias : SKFontEdging.SubpixelAntialias;
                    break;
            }

            return _textBlobCache.GetOrAdd(edging, (_) =>
            {
                var font = _glyphTypefaceImpl.SKFont;

                font.Hinting = SKFontHinting.Full;
                font.Subpixel = edging == SKFontEdging.SubpixelAntialias;
                font.Edging = edging;
                font.Size = (float)FontRenderingEmSize;

                var builder = SKTextBlobBuilderCache.Shared.Get();

                var runBuffer = builder.AllocatePositionedRun(font, _glyphIndices.Length);

                runBuffer.SetPositions(_glyphPositions);
                runBuffer.SetGlyphs(_glyphIndices);

                var textBlob = builder.Build();

                SKTextBlobBuilderCache.Shared.Return(builder);

                return textBlob;
            });
        }

        public void Dispose()
        {
            foreach (var pair in _textBlobCache)
            {
                pair.Value.Dispose();
            }
        }

        public IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit)
        {
            var textBlob = GetTextBlob(default);

            return textBlob.GetIntercepts(lowerLimit, upperLimit);
        }
    }
}
