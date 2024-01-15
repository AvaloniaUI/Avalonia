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

            if (glyphInfos == null)
            {
                throw new ArgumentNullException(nameof(glyphInfos));
            }

            _glyphTypefaceImpl = (GlyphTypefaceImpl)glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;

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

            // Ideally the requested edging should be passed to the glyph run.
            // Currently the edging is computed dynamically inside the drawing context, so we can't know it in advance.
            // But the bounds depends on the edging: for now, always use SubpixelAntialias so we have consistent values.
            // The resulting bounds may be shifted by 1px on some fonts:
            // "F" text with Inter size 14 has a 0px left bound with SubpixelAntialias but 1px with Antialias.
            using var font = CreateFont(SKFontEdging.SubpixelAntialias);

            var runBounds = new Rect();
            var glyphBounds = ArrayPool<SKRect>.Shared.Rent(count);

            font.GetGlyphWidths(_glyphIndices, null, glyphBounds.AsSpan(0, count));

            currentX = 0;

            for (var i = 0; i < count; i++)
            {
                var gBounds = glyphBounds[i];
                var advance = glyphInfos[i].GlyphAdvance;

                runBounds = runBounds.Union(new Rect(currentX + gBounds.Left, baselineOrigin.Y + gBounds.Top, gBounds.Width, gBounds.Height));

                currentX += advance;
            }

            ArrayPool<SKRect>.Shared.Return(glyphBounds);

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
                using var font = CreateFont(edging);

                var builder = SKTextBlobBuilderCache.Shared.Get();

                var runBuffer = builder.AllocatePositionedRun(font, _glyphIndices.Length);

                runBuffer.SetPositions(_glyphPositions);
                runBuffer.SetGlyphs(_glyphIndices);

                var textBlob = builder.Build();

                SKTextBlobBuilderCache.Shared.Return(builder);

                return textBlob;
            });
        }

        private SKFont CreateFont(SKFontEdging edging)
        {
            var font = _glyphTypefaceImpl.CreateSKFont((float)FontRenderingEmSize);

            font.Hinting = SKFontHinting.Full;
            font.Subpixel = edging != SKFontEdging.Alias;
            font.Edging = edging;

            return font;
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
