using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
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
            var skFont = new SKFont(glyphTypeface.Typeface, fontRenderingEmSize)
            {
                Size = fontRenderingEmSize,
                Edging = SKFontEdging.Alias,
                Hinting = SKFontHinting.None,
                LinearMetrics = true
            };

            SKPath path = new SKPath();

            var (currentX, currentY) = glyphRun.BaselineOrigin;

            for (var i = 0; i < glyphRun.GlyphIndices.Count; i++)
            {
                var glyph = glyphRun.GlyphIndices[i];
                var glyphPath = skFont.GetGlyphPath(glyph);

                if (!glyphPath.IsEmpty)
                {
                    path.AddPath(glyphPath, (float)currentX, (float)currentY);
                }

                if (glyphRun.GlyphAdvances != null)
                {
                    currentX += glyphRun.GlyphAdvances[i];
                }
                else
                {
                    currentX += glyphPath.Bounds.Right;
                }
            }

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

        public IGlyphRunImpl CreateGlyphRun(IGlyphTypeface glyphTypeface, double fontRenderingEmSize, IReadOnlyList<ushort> glyphIndices,
            IReadOnlyList<double> glyphAdvances, IReadOnlyList<Vector> glyphOffsets)
        {
            if (glyphTypeface == null)
            {
                throw new ArgumentNullException(nameof(glyphTypeface));
            }

            if (glyphIndices == null)
            {
                throw new ArgumentNullException(nameof(glyphIndices));
            }

            var glyphTypefaceImpl = glyphTypeface as GlyphTypefaceImpl;

            var font = new SKFont
            {
                LinearMetrics = true,
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias,
                Hinting = SKFontHinting.Full,
                Size = (float)fontRenderingEmSize,
                Typeface = glyphTypefaceImpl.Typeface,
                Embolden = (glyphTypefaceImpl.FontSimulations & FontSimulations.Bold) != 0,
                SkewX = (glyphTypefaceImpl.FontSimulations & FontSimulations.Oblique) != 0 ? -0.2f : 0
            };

            var builder = new SKTextBlobBuilder();

            var count = glyphIndices.Count;

            if (glyphOffsets != null && glyphAdvances != null)
            {
                var runBuffer = builder.AllocatePositionedRun(font, count);

                var glyphSpan = runBuffer.GetGlyphSpan();
                var positionSpan = runBuffer.GetPositionSpan();

                var currentX = 0.0;

                for (int i = 0; i < glyphOffsets.Count; i++)
                {
                    var offset = glyphOffsets[i];

                    glyphSpan[i] = glyphIndices[i];

                    positionSpan[i] = new SKPoint((float)(currentX + offset.X), (float)offset.Y);

                    currentX += glyphAdvances[i];
                }
            }
            else
            {
                if (glyphAdvances != null)
                {
                    var runBuffer = builder.AllocateHorizontalRun(font, count, 0);

                    var glyphSpan = runBuffer.GetGlyphSpan();
                    var positionSpan = runBuffer.GetPositionSpan();

                    var currentX = 0.0;

                    for (int i = 0; i < glyphAdvances.Count; i++)
                    {
                        glyphSpan[i] = glyphIndices[i];

                        positionSpan[i] = (float)currentX;

                        currentX += glyphAdvances[i];
                    }
                }
                else
                {
                    var runBuffer = builder.AllocateRun(font, count, 0, 0);

                    var glyphSpan = runBuffer.GetGlyphSpan();

                    for (int i = 0; i < glyphIndices.Count; i++)
                    {
                        glyphSpan[i] = glyphIndices[i];
                    }
                }
            }

            return new GlyphRunImpl(builder.Build());
        }
    }
}
