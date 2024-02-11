using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Avalonia.Media.TextFormatting;
using Avalonia.Metal;
using Avalonia.Skia.Metal;

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


        public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext? graphicsContext)
        {
            if (graphicsContext == null)
                return new SkiaContext(null);
            if (graphicsContext is ISkiaGpu skiaGpu)
                return new SkiaContext(skiaGpu);
            if (graphicsContext is IGlContext gl)
                return new SkiaContext(new GlSkiaGpu(gl, _maxResourceBytes));
            if (graphicsContext is IMetalDevice metal)
                return new SkiaContext(new SkiaMetalGpu(metal, _maxResourceBytes));
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

        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<IGeometryImpl> children)
        {
            return new GeometryGroupImpl(fillRule, children);
        }

        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, IGeometryImpl g1, IGeometryImpl g2)
        {
            return CombinedGeometryImpl.ForceCreate(combineMode, g1, g2);
        }

        public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun)
        {
            if (glyphRun.GlyphTypeface is not GlyphTypefaceImpl glyphTypeface)
            {
                throw new InvalidOperationException("PlatformImpl can't be null.");
            }

            var fontRenderingEmSize = (float)glyphRun.FontRenderingEmSize;

            using var skFont = glyphTypeface.CreateSKFont(fontRenderingEmSize);

            skFont.Hinting = SKFontHinting.None;

            SKPath path = new SKPath();

            var (currentX, currentY) = glyphRun.BaselineOrigin;

            for (var i = 0; i < glyphRun.GlyphInfos.Count; i++)
            {
                var glyph = glyphRun.GlyphInfos[i].GlyphIndex;
                var glyphPath = skFont.GetGlyphPath(glyph);

                if (glyphPath is not null && !glyphPath.IsEmpty)
                {
                    path.AddPath(glyphPath, (float)currentX, (float)currentY);
                }

                currentX += glyphRun.GlyphInfos[i].GlyphAdvance;
            }

            return new StreamGeometryImpl(path, path);
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

            return new RenderTargetBitmapImpl(size, dpi);
        }

        /// <inheritdoc />
        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            return new WriteableBitmapImpl(size, dpi, format, alphaFormat);
        }

        public IGlyphRunImpl CreateGlyphRun(IGlyphTypeface glyphTypeface, double fontRenderingEmSize, 
            IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin)
        {
            return new GlyphRunImpl(glyphTypeface, fontRenderingEmSize, glyphInfos, baselineOrigin);
        }
    }
}
