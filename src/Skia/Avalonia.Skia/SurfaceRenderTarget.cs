using System;
using System.IO;
using Avalonia.Reactive;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia render target that writes to a surface.
    /// </summary>
    internal class SurfaceRenderTarget : IDrawingContextLayerImpl, IDrawableBitmapImpl
    {
        private readonly ISkiaSurface _surface;
        private readonly SKCanvas _canvas;
        private readonly bool _disableLcdRendering;
        private readonly GRContext? _grContext;
        private readonly ISkiaGpu? _gpu;

        private class SkiaSurfaceWrapper : ISkiaSurface
        {
            private SKSurface? _surface;

            public SKSurface Surface => _surface ?? throw new ObjectDisposedException(nameof(SkiaSurfaceWrapper));
            public bool CanBlit => false;
            public void Blit(SKCanvas canvas) => throw new NotSupportedException();

            public SkiaSurfaceWrapper(SKSurface surface)
            {
                _surface = surface;
            }

            public void Dispose()
            {
                _surface?.Dispose();
                _surface = null;
            }
        }
        
        /// <summary>
        /// Create new surface render target.
        /// </summary>
        /// <param name="createInfo">Create info.</param>
        public SurfaceRenderTarget(CreateInfo createInfo)
        {
            PixelSize = new PixelSize(createInfo.Width, createInfo.Height);
            Dpi = createInfo.Dpi;

            _disableLcdRendering = createInfo.DisableTextLcdRendering;
            _grContext = createInfo.GrContext;
            _gpu = createInfo.Gpu;

            ISkiaSurface? surface = null;

            if (!createInfo.DisableManualFbo)
                surface = _gpu?.TryCreateSurface(PixelSize, createInfo.Session);

            if (surface is null)
            {
                if (CreateSurface(createInfo.GrContext, PixelSize.Width, PixelSize.Height, createInfo.Format)
                    is { } skSurface)
                {
                    surface = new SkiaSurfaceWrapper(skSurface);
                }
            }

            if (surface?.Surface.Canvas is not { } canvas)
                throw new InvalidOperationException("Failed to create Skia render target surface");

            _surface = surface;
            _canvas = canvas;
        }

        /// <summary>
        /// Create backing Skia surface.
        /// </summary>
        /// <param name="gpu">GPU.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="format">Format.</param>
        /// <returns></returns>
        private static SKSurface? CreateSurface(GRContext? gpu, int width, int height, PixelFormat? format)
        {
            var imageInfo = MakeImageInfo(width, height, format);
            if (gpu != null)
                return SKSurface.Create(gpu, false, imageInfo, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));
            return SKSurface.Create(imageInfo, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _canvas.Dispose();
            _surface.Dispose();
        }

        /// <inheritdoc />
        public IDrawingContextImpl CreateDrawingContext()
        {
            _canvas.RestoreToCount(-1);
            _canvas.ResetMatrix();
            
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Surface = _surface.Surface,
                Dpi = Dpi,
                DisableSubpixelTextRendering = _disableLcdRendering,
                GrContext = _grContext,
                Gpu = _gpu,
            };

            return new DrawingContextImpl(createInfo, Disposable.Create(() => Version++));
        }

        public bool IsCorrupted => _gpu?.IsLost == true;

        /// <inheritdoc />
        public Vector Dpi { get; }

        /// <inheritdoc />
        public PixelSize PixelSize { get; }

        public int Version { get; private set; } = 1;

        /// <inheritdoc />
        public void Save(string fileName, int? quality = null)
        {
            using (var image = SnapshotImage())
            {
                ImageSavingHelper.SaveImage(image, fileName, quality);
            }
        }

        /// <inheritdoc />
        public void Save(Stream stream, int? quality = null)
        {
            using (var image = SnapshotImage())
            {
                ImageSavingHelper.SaveImage(image, stream, quality);
            }
        }

        public void Blit(IDrawingContextImpl contextImpl)
        {
            var context = (DrawingContextImpl)contextImpl;

            if (_surface.CanBlit)
            {
                _surface.Surface.Canvas.Flush();
                _surface.Blit(context.Canvas);
            }
            else
            {
                var oldMatrix = context.Canvas.TotalMatrix;
                context.Canvas.ResetMatrix();
                _surface.Surface.Draw(context.Canvas, 0, 0, null);
                context.Canvas.SetMatrix(oldMatrix);
            }
        }

        public bool CanBlit => true;

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            using var image = SnapshotImage();
            context.Canvas.DrawImage(image, sourceRect, destRect, paint);
        }
        
        /// <summary>
        /// Create Skia image snapshot from a surface.
        /// </summary>
        /// <returns>Image snapshot.</returns>
        public SKImage SnapshotImage()
        {
            return _surface.Surface.Snapshot();
        }

        /// <summary>
        /// Create image info for given parameters.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="format">Format.</param>
        /// <returns></returns>
        private static SKImageInfo MakeImageInfo(int width, int height, PixelFormat? format)
        {
            var colorType = PixelFormatHelper.ResolveColorType(format);

            return new SKImageInfo(Math.Max(width, 1), Math.Max(height, 1), colorType, SKAlphaType.Premul);
        }

        /// <summary>
        /// Create info of a surface render target.
        /// </summary>
        public struct CreateInfo
        {
            /// <summary>
            /// Width of a render target.
            /// </summary>
            public int Width;

            /// <summary>
            /// Height of a render target.
            /// </summary>
            public int Height;

            /// <summary>
            /// Dpi used when rendering to a surface.
            /// </summary>
            public Vector Dpi;

            /// <summary>
            /// Pixel format of a render target.
            /// </summary>
            public PixelFormat? Format;

            /// <summary>
            /// Render text without Lcd rendering.
            /// </summary>
            public bool DisableTextLcdRendering;

            /// <summary>
            /// GPU-accelerated context (optional)
            /// </summary>
            public GRContext? GrContext;

            public ISkiaGpu? Gpu;

            public ISkiaGpuRenderSession? Session;

            public bool DisableManualFbo;
        }
    }
}
