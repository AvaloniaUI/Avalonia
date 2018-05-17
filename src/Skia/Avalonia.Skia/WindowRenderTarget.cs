// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Skia.Gpu;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia render target that renders to a window using Gpu acceleration.
    /// </summary>
    public class WindowRenderTarget : IRenderTarget
    {
        private readonly IGpuRenderContext _renderContext;
        private readonly IDisposable _postRenderHandler;
        private GRBackendRenderTargetDesc _rtDesc;
        private SKSurface _surface;
        private SKCanvas _canvas;
        private Size _surfaceDpi;
        
        /// <summary>
        /// Create new window render target that will render to given handle using passed render backend.
        /// </summary>
        public WindowRenderTarget(IGpuRenderContext renderContext)
        {
            _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
            
            _rtDesc = CreateInitialRenderTargetDesc();
            _postRenderHandler = new LambdaDisposable(Flush);
        }

        private GRBackendRenderTargetDesc CreateInitialRenderTargetDesc()
        {
            _renderContext.PrepareForRendering();

            var framebufferDesc = _renderContext.GetFramebufferParameters();
            
            var pixelConfig = SKImageInfo.PlatformColorType == SKColorType.Bgra8888
                ? GRPixelConfig.Bgra8888
                : GRPixelConfig.Rgba8888;

            var rtDesc = new GRBackendRenderTargetDesc
            {
                Width = 0,
                Height = 0,
                Config = pixelConfig,
                Origin = GRSurfaceOrigin.BottomLeft,
                SampleCount = framebufferDesc.SampleCount,
                StencilBits = framebufferDesc.StencilBits,
                RenderTargetHandle = framebufferDesc.FramebufferHandle
            };

            return rtDesc;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _renderContext.PrepareForRendering();

             _canvas?.Dispose();
            _surface?.Dispose();
            _renderContext.Dispose();
        }
        
        /// <summary>
        /// Flush rendering commands and present.
        /// </summary>
        private void Flush()
        {
            _renderContext.Context.Flush();
            _renderContext.Present();
        }

        /// <summary>
        /// Create surface if needed.
        /// </summary>
        private void CreateSurface()
        {
            var newSize = _renderContext.GetFramebufferSize();
            var newWidth = (int)newSize.Width;
            var newHeight = (int)newSize.Height;

            var newDpi = _renderContext.GetFramebufferDpi();

            if (_surface == null || newWidth != _rtDesc.Width || newHeight != _rtDesc.Height || newDpi != _surfaceDpi)
            {
                _renderContext.RecreateSurface();
                _renderContext.PrepareForRendering();
                
                _canvas?.Dispose();
                _surface?.Dispose();

                _rtDesc.Width = newWidth;
                _rtDesc.Height = newHeight;
                _surfaceDpi = newDpi;
                
                _surface = SKSurface.Create(_renderContext.Context, _rtDesc);

                _canvas = _surface?.Canvas;

                if (_surface == null || _canvas == null)
                {
                    throw new InvalidOperationException("Failed to create Skia surface for window render target");
                }
            }
        }

        /// <inheritdoc />
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            _renderContext.PrepareForRendering();

            CreateSurface();
            
            _canvas.RestoreToCount(-1);
            _canvas.ResetMatrix();

            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = _canvas,
                Dpi = new Vector(_surfaceDpi.Width, _surfaceDpi.Height),
                VisualBrushRenderer = visualBrushRenderer,
                RenderContext = _renderContext
            };

            return new DrawingContextImpl(createInfo, _postRenderHandler);
        }
    }
}