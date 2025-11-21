using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
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

        // A two level cache optimized for single-entry read. Uses TextOptions as a key.
        private readonly TwoLevelCache<TextOptions, SKTextBlob> _textBlobCache =
            new TwoLevelCache<TextOptions, SKTextBlob>(secondarySize: 3, evictionAction: b => b?.Dispose());

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
            var defaultTextOptions = default(TextOptions) with 
            { 
                TextRenderingMode = TextRenderingMode.SubpixelAntialias, 
                TextHintingMode = TextHintingMode.Strong, 
                BaselinePixelAlign = true 
            };

            using var font = CreateFont(defaultTextOptions);

            var runBounds = new Rect();
            var glyphBounds = ArrayPool<SKRect>.Shared.Rent(count);

            font.GetGlyphWidths(_glyphIndices, null, glyphBounds.AsSpan(0, count));

            currentX = 0;

            for (var i = 0; i < count; i++)
            {
                var gBounds = glyphBounds[i];
                var advance = glyphInfos[i].GlyphAdvance;

                runBounds = runBounds.Union(new Rect(currentX + gBounds.Left, gBounds.Top, gBounds.Width, gBounds.Height));

                currentX += advance;
            }
            ArrayPool<SKRect>.Shared.Return(glyphBounds);

            BaselineOrigin = baselineOrigin;
            Bounds = runBounds.Translate(new Vector(baselineOrigin.X, baselineOrigin.Y));
        }

        public IGlyphTypeface GlyphTypeface => _glyphTypefaceImpl;

        public double FontRenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public Rect Bounds { get; }

        public SKTextBlob GetTextBlob(TextOptions textOptions, RenderOptions renderOptions)
        {
            if (textOptions.TextRenderingMode == TextRenderingMode.Unspecified)
            {
                textOptions = textOptions with 
                { 
                    TextRenderingMode = renderOptions.EdgeMode == EdgeMode.Aliased ? TextRenderingMode.Alias : TextRenderingMode.SubpixelAntialias 
                };
            }
            if (!textOptions.BaselinePixelAlign.HasValue)
            {
                textOptions = textOptions with { BaselinePixelAlign = true };
            }

            return _textBlobCache.GetOrAdd(textOptions, k =>
            {
                using var font = CreateFont(textOptions);

                var builder = SKTextBlobBuilderCache.Shared.Get();

                var runBuffer = builder.AllocatePositionedRun(font, _glyphIndices.Length);

                runBuffer.SetPositions(_glyphPositions);
                runBuffer.SetGlyphs(_glyphIndices);

                var textBlob = builder.Build()!;
                SKTextBlobBuilderCache.Shared.Return(builder);
                return textBlob;
            });
        }

        private SKFont CreateFont(TextOptions textOptions)
        {
            // Determine edging from TextRenderingMode
            var edging = textOptions.TextRenderingMode switch
            {
                TextRenderingMode.Alias => SKFontEdging.Alias,
                TextRenderingMode.Antialias => SKFontEdging.Antialias,
                TextRenderingMode.SubpixelAntialias => SKFontEdging.SubpixelAntialias,
                _ => SKFontEdging.SubpixelAntialias
            };

            // Determine hinting
            var hinting = textOptions.TextHintingMode switch
            {
                TextHintingMode.None => SKFontHinting.None,
                TextHintingMode.Light => SKFontHinting.Slight,
                TextHintingMode.Strong => SKFontHinting.Full,
                _ => SKFontHinting.Full,
            };

            // Force auto-hinting for "Slight" mode (prefer autohinter over bytecode hints), otherwise default.
            var forceAutoHinting = textOptions.TextHintingMode == TextHintingMode.Light;

            // Subpixel rendering enabled when edging is not alias.
            var subpixel = edging != SKFontEdging.Alias;

            // Baseline snap defaults to true unless explicitly disabled.
            var baselineSnap = textOptions.BaselinePixelAlign.GetValueOrDefault(true);

            var font = _glyphTypefaceImpl.CreateSKFont((float)FontRenderingEmSize);

            font.ForceAutoHinting = forceAutoHinting;
            font.Hinting = hinting;
            font.Subpixel = subpixel;
            font.Edging = edging;
            font.BaselineSnap = baselineSnap;

            return font;
        }

        public void Dispose()
        {
            _textBlobCache.ClearAndDispose();
        }

        public IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit)
        {
            var textBlob = GetTextBlob(default, default);

            return textBlob.GetIntercepts(lowerLimit, upperLimit);
        }
    }
}
