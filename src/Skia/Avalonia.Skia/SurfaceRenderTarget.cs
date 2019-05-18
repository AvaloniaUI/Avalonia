// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia render target that writes to a surface.
    /// </summary>
    internal class SurfaceRenderTarget : IRenderTargetBitmapImpl, IDrawableBitmapImpl
    {
        private readonly SKSurface _surface;
        private readonly SKCanvas _canvas;
        private readonly bool _disableLcdRendering;
        private readonly GRContext _grContext;
        
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
            _surface = CreateSurface(createInfo.GrContext, PixelSize.Width, PixelSize.Height, createInfo.Format);

            _canvas = _surface?.Canvas;

            if (_surface == null || _canvas == null)
            {
                throw new InvalidOperationException("Failed to create Skia render target surface");
            }
        }

        /// <summary>
        /// Create backing Skia surface.
        /// </summary>
        /// <param name="gpu">GPU.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="format">Format.</param>
        /// <returns></returns>
        private static SKSurface CreateSurface(GRContext gpu, int width, int height, PixelFormat? format)
        {
            var imageInfo = MakeImageInfo(width, height, format);
            if (gpu != null)
                return SKSurface.Create(gpu, false, imageInfo);
            return SKSurface.Create(imageInfo);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _canvas.Dispose();
            _surface.Dispose();
        }

        /// <inheritdoc />
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            _canvas.RestoreToCount(-1);
            _canvas.ResetMatrix();
            
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = _canvas,
                Dpi = Dpi,
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = _disableLcdRendering,
                GrContext = _grContext
            };

            return new DrawingContextImpl(createInfo, Disposable.Create(() => Version++));
        }

        /// <inheritdoc />
        public Vector Dpi { get; }

        /// <inheritdoc />
        public PixelSize PixelSize { get; }

        public int Version { get; private set; } = 1;

        /// <inheritdoc />
        public void Save(string fileName)
        {
            using (var image = SnapshotImage())
            {
                ImageSavingHelper.SaveImage(image, fileName);
            }
        }

        /// <inheritdoc />
        public void Save(Stream stream)
        {
            using (var image = SnapshotImage())
            {
                ImageSavingHelper.SaveImage(image, stream);
            }
        }

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            using (var image = SnapshotImage())
            {
                context.Canvas.DrawImage(image, sourceRect, destRect, paint);
            }
        }
        
        /// <summary>
        /// Create Skia image snapshot from a surface.
        /// </summary>
        /// <returns>Image snapshot.</returns>
        public SKImage SnapshotImage()
        {
            return _surface.Snapshot();
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
            public GRContext GrContext;
        }
    }
}
