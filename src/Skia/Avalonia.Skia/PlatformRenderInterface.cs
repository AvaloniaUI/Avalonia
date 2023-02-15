using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia platform render interface.
    /// </summary>
    internal class PlatformRenderInterface : IPlatformRenderInterface
    {
        private readonly long? _maxResourceBytes;

        public PlatformRenderInterface(long? maxResourceBytes = null)
        {
            _maxResourceBytes = maxResourceBytes;
            DefaultPixelFormat = SKImageInfo.PlatformColorType.ToPixelFormat();
        }


        public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext graphicsContext)
        {
            if (graphicsContext == null)
                return new SkiaContext(null);
            if (graphicsContext is ISkiaGpu skiaGpu)
                return new SkiaContext(skiaGpu);
            if (graphicsContext is IGlContext gl)
                return new SkiaContext(new GlSkiaGpu(gl, _maxResourceBytes));
            throw new ArgumentException("Graphics context of type is not supported");
        }

        public bool SupportsIndividualRoundRects => true;

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat { get; }

        public bool IsSupportedBitmapPixelFormat(PixelFormat format) =>
            format == PixelFormats.Rgb565
            || format == PixelFormats.Bgra8888
            || format == PixelFormats.Rgba8888;

        public IGeometryImpl CreateEllipseGeometry(Rect rect) => new EllipseGeometryImpl(rect);

        public IGeometryImpl CreateLineGeometry(Point p1, Point p2) => new LineGeometryImpl(p1, p2);

        public IGeometryImpl CreateRectangleGeometry(Rect rect) => new RectangleGeometryImpl(rect);

        /// <inheritdoc />
        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<Geometry> children)
        {
            return new GeometryGroupImpl(fillRule, children);
        }

        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, Geometry g1, Geometry g2)
        {
            return new CombinedGeometryImpl(combineMode, g1, g2);
        }

        public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun)
        {
            if (glyphRun.GlyphTypeface is not GlyphTypefaceImpl glyphTypeface)
            {
                throw new InvalidOperationException("PlatformImpl can't be null.");
            }

            var fontRenderingEmSize = (float)glyphRun.FontRenderingEmSize;

            var skFont = SKFontCache.Shared.Get();

            skFont.Typeface = glyphTypeface.Typeface;
            skFont.Size = fontRenderingEmSize;
            skFont.Edging = SKFontEdging.Alias;
            skFont.Hinting = SKFontHinting.None;
            skFont.LinearMetrics = true;

            SKPath path = new SKPath();

            var (currentX, currentY) = glyphRun.BaselineOrigin;

            for (var i = 0; i < glyphRun.GlyphInfos.Count; i++)
            {
                var glyph = glyphRun.GlyphInfos[i].GlyphIndex;
                var glyphPath = skFont.GetGlyphPath(glyph);

                if (!glyphPath.IsEmpty)
                {
                    path.AddPath(glyphPath, (float)currentX, (float)currentY);
                }

                currentX += glyphRun.GlyphInfos[i].GlyphAdvance;
            }

            SKFontCache.Shared.Return(skFont);

            return new StreamGeometryImpl(path);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return LoadBitmap(stream);
            }
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new ImmutableBitmap(stream);
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WriteableBitmapImpl(stream, width, true, interpolationMode);
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WriteableBitmapImpl(stream, height, false, interpolationMode);
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return LoadWriteableBitmap(stream);
            }
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream)
        {
            return new WriteableBitmapImpl(stream);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            return new ImmutableBitmap(size, dpi, stride, format, alphaFormat, data);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new ImmutableBitmap(stream, width, true, interpolationMode);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new ImmutableBitmap(stream, height, false, interpolationMode);
        }

        /// <inheritdoc />
        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            if (bitmapImpl is ImmutableBitmap ibmp)
            {
                return new ImmutableBitmap(ibmp, destinationSize, interpolationMode);
            }
            else
            {
                throw new Exception("Invalid source bitmap type.");
            }
        }

        /// <inheritdoc />
        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            if (size.Width < 1)
            {
                throw new ArgumentException("Width can't be less than 1", nameof(size));
            }

            if (size.Height < 1)
            {
                throw new ArgumentException("Height can't be less than 1", nameof(size));
            }

            var createInfo = new SurfaceRenderTarget.CreateInfo
            {
                Width = size.Width,
                Height = size.Height,
                Dpi = dpi,
                DisableTextLcdRendering = false,
                DisableManualFbo = true,
            };

            return new SurfaceRenderTarget(createInfo);
        }

        /// <inheritdoc />
        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            return new WriteableBitmapImpl(size, dpi, format, alphaFormat);
        }

        public IGlyphRunImpl CreateGlyphRun(
            IGlyphTypeface glyphTypeface,
            double fontRenderingEmSize, 
            IReadOnlyList<GlyphInfo> glyphInfos,
            Point baselineOrigin)
        {
            if (glyphTypeface == null)
            {
                throw new ArgumentNullException(nameof(glyphTypeface));
            }

            if (glyphInfos == null)
            {
                throw new ArgumentNullException(nameof(glyphInfos));
            }

            var glyphTypefaceImpl = glyphTypeface as GlyphTypefaceImpl;

            var font = SKFontCache.Shared.Get();

            font.LinearMetrics = true;
            font.Subpixel = true;
            font.Edging = SKFontEdging.SubpixelAntialias;
            font.Hinting = SKFontHinting.Full;
            font.Size = (float)fontRenderingEmSize;
            font.Typeface = glyphTypefaceImpl.Typeface;
            font.Embolden = (glyphTypefaceImpl.FontSimulations & FontSimulations.Bold) != 0;
            font.SkewX = (glyphTypefaceImpl.FontSimulations & FontSimulations.Oblique) != 0 ? -0.2f : 0;


            var builder = SKTextBlobBuilderCache.Shared.Get();
            var count = glyphInfos.Count;

            var runBuffer = builder.AllocatePositionedRun(font, count);

            var glyphSpan = runBuffer.GetGlyphSpan();
            var positionSpan = runBuffer.GetPositionSpan();

            SKFontCache.Shared.Return(font);

            var width = 0.0;

            for (int i = 0; i < count; i++)
            {
                var glyphInfo = glyphInfos[i];
                var offset = glyphInfo.GlyphOffset;

                glyphSpan[i] = glyphInfo.GlyphIndex;

                positionSpan[i] = new SKPoint((float)(width + offset.X), (float)offset.Y);

                width += glyphInfo.GlyphAdvance;
            }

            var scale = fontRenderingEmSize / glyphTypeface.Metrics.DesignEmHeight;
            var height = glyphTypeface.Metrics.LineSpacing * scale;
            var skTextBlob = builder.Build();

            SKTextBlobBuilderCache.Shared.Return(builder);

            return new GlyphRunImpl(skTextBlob, new Size(width, height), baselineOrigin);
        }
    }
}
