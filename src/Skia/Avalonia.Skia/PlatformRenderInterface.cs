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
using System.Runtime.InteropServices;
using System.Drawing;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia platform render interface.
    /// </summary>
    internal class PlatformRenderInterface : IPlatformRenderInterface, IOpenGlAwarePlatformRenderInterface
    {
        private readonly ISkiaGpu _skiaGpu;

        public PlatformRenderInterface(ISkiaGpu skiaGpu, long? maxResourceBytes = null)
        {
            DefaultPixelFormat = SKImageInfo.PlatformColorType.ToPixelFormat();

            if (skiaGpu != null)
            {
                _skiaGpu = skiaGpu;
                return;
            }

            var gl = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();
            if (gl != null)
                _skiaGpu = new GlSkiaGpu(gl, maxResourceBytes);
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
            if (glyphRun.GlyphTypeface.PlatformImpl is not GlyphTypefaceImpl glyphTypeface)
            {
                throw new InvalidOperationException("PlatformImpl can't be null.");
            }

            var fontRenderingEmSize = (float)glyphRun.FontRenderingEmSize;
            var skFont = new SKFont(glyphTypeface.Typeface, fontRenderingEmSize)
            {
                Size = fontRenderingEmSize,
                Edging = SKFontEdging.Antialias,
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
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            if (!(surfaces is IList))
                surfaces = surfaces.ToList();
            var gpuRenderTarget = _skiaGpu?.TryCreateRenderTarget(surfaces);
            if (gpuRenderTarget != null)
            {
                return new SkiaGpuRenderTarget(_skiaGpu, gpuRenderTarget);
            }

            foreach (var surface in surfaces)
            {
                if (surface is IFramebufferPlatformSurface framebufferSurface)
                    return new FramebufferRenderTarget(framebufferSurface);
            }

            throw new NotSupportedException(
                "Don't know how to create a Skia render target from any of provided surfaces");
        }

        /// <inheritdoc />
        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            return new WriteableBitmapImpl(size, dpi, format, alphaFormat);
        }

        public IOpenGlBitmapImpl CreateOpenGlBitmap(PixelSize size, Vector dpi)
        {
            if (_skiaGpu is IOpenGlAwareSkiaGpu glAware)
                return glAware.CreateOpenGlBitmap(size, dpi);
            if (_skiaGpu == null)
                throw new PlatformNotSupportedException("GPU acceleration is not available");
            throw new PlatformNotSupportedException(
                "Current GPU acceleration backend does not support OpenGL integration");
        }

        public IGlyphRunBuffer AllocateGlyphRun(GlyphTypeface glyphTypeface, float fontRenderingEmSize, int length) 
            => new SKGlyphRunBuffer(glyphTypeface, fontRenderingEmSize, length);

        public IHorizontalGlyphRunBuffer AllocateHorizontalGlyphRun(GlyphTypeface glyphTypeface, float fontRenderingEmSize, int length) 
            => new SKHorizontalGlyphRunBuffer(glyphTypeface, fontRenderingEmSize, length);

        public IPositionedGlyphRunBuffer AllocatePositionedGlyphRun(GlyphTypeface glyphTypeface, float fontRenderingEmSize, int length) 
            => new SKPositionedGlyphRunBuffer(glyphTypeface, fontRenderingEmSize, length);

        private abstract class SKGlyphRunBufferBase : IGlyphRunBuffer
        {
            protected readonly SKTextBlobBuilder _builder;
            protected readonly SKFont _font;

            public SKGlyphRunBufferBase(GlyphTypeface glyphTypeface, float fontRenderingEmSize, int length)
            {
                _builder = new SKTextBlobBuilder();

                var glyphTypefaceImpl = (GlyphTypefaceImpl)glyphTypeface.PlatformImpl;

                _font = new SKFont
                {
                    Subpixel = true,
                    Edging = SKFontEdging.SubpixelAntialias,
                    Hinting = SKFontHinting.Full,
                    LinearMetrics = true,                   
                    Size = fontRenderingEmSize,
                    Typeface = glyphTypefaceImpl.Typeface,
                    Embolden = glyphTypefaceImpl.IsFakeBold,
                    SkewX = glyphTypefaceImpl.IsFakeItalic ? -0.2f : 0
                };
            }

            public abstract Span<ushort> GlyphIndices { get; }

            public IGlyphRunImpl Build()
            {
                return new GlyphRunImpl(_builder.Build());
            }
        }

        private sealed class SKGlyphRunBuffer : SKGlyphRunBufferBase
        {
            private readonly SKRunBuffer _buffer;

            public SKGlyphRunBuffer(GlyphTypeface glyphTypeface, float fontRenderingEmSize, int length) : base(glyphTypeface, fontRenderingEmSize, length)
            {
                _buffer = _builder.AllocateRun(_font, length, 0, 0);
            }

            public override Span<ushort> GlyphIndices => _buffer.GetGlyphSpan();
        }

        private sealed class SKHorizontalGlyphRunBuffer : SKGlyphRunBufferBase, IHorizontalGlyphRunBuffer
        {
            private readonly SKHorizontalRunBuffer _buffer;

            public SKHorizontalGlyphRunBuffer(GlyphTypeface glyphTypeface, float fontRenderingEmSize, int length) : base(glyphTypeface, fontRenderingEmSize, length)
            {
                _buffer = _builder.AllocateHorizontalRun(_font, length, 0);
            }

            public override Span<ushort> GlyphIndices => _buffer.GetGlyphSpan();

            public Span<float> GlyphPositions => _buffer.GetPositionSpan();
        }

        private sealed class SKPositionedGlyphRunBuffer : SKGlyphRunBufferBase, IPositionedGlyphRunBuffer
        {
            private readonly SKPositionedRunBuffer _buffer;

            public SKPositionedGlyphRunBuffer(GlyphTypeface glyphTypeface, float fontRenderingEmSize, int length) : base(glyphTypeface, fontRenderingEmSize, length)
            {
                _buffer = _builder.AllocatePositionedRun(_font, length);
            }

            public override Span<ushort> GlyphIndices => _buffer.GetGlyphSpan();

            public Span<PointF> GlyphPositions => MemoryMarshal.Cast<SKPoint, PointF>(_buffer.GetPositionSpan());
        }
    }
}
