// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// A bitmap that holds the rendering of an <see cref="IVisual"/>.
    /// </summary>
    public class RenderTargetBitmap : Bitmap, IDisposable, IRenderTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTargetBitmap"/> class.
        /// </summary>
        /// <param name="pixelWidth">The width of the bitmap in pixels.</param>
        /// <param name="pixelHeight">The height of the bitmap in pixels.</param>
        /// <param name="dpiX">The horizontal DPI of the bitmap.</param>
        /// <param name="dpiY">The vertical DPI of the bitmap.</param>
        public RenderTargetBitmap(int pixelWidth, int pixelHeight, double dpiX = 96, double dpiY = 96)
            : this(CreateImpl(pixelWidth, pixelHeight, dpiX, dpiY))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTargetBitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific render target bitmap implementation.</param>
        protected RenderTargetBitmap(IRenderTargetBitmapImpl impl)
            : base(impl)
        {
        }

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        public new IRenderTargetBitmapImpl PlatformImpl => (IRenderTargetBitmapImpl)base.PlatformImpl;

        /// <summary>
        /// Disposes of the bitmap.
        /// </summary>
        public void Dispose()
        {
            PlatformImpl.Dispose();
        }

        /// <summary>
        /// Renders a visual to the <see cref="RenderTargetBitmap"/>.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        public void Render(IVisual visual) => ImmediateRenderer.Render(visual, this);

        /// <inheritdoc/>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer vbr) => PlatformImpl.CreateDrawingContext(vbr);

        public RenderTargetBitmap CreateLayer(int pixelWidth, int pixelHeight)
        {
            return new RenderTargetBitmap(CreateLayerImpl(pixelWidth, pixelHeight));
        }

        IRenderTargetBitmapImpl IRenderTarget.CreateLayer(int pixelWidth, int pixelHeight)
        {
            return CreateLayerImpl(pixelWidth, pixelHeight);
        }

        private static IRenderTargetBitmapImpl CreateImpl(int pixelWidth, int pixelHeight, double dpiX, double dpiY)
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return factory.CreateRenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY);
        }

        private IRenderTargetBitmapImpl CreateLayerImpl(int pixelWidth, int pixelHeight)
        {
            return PlatformImpl.CreateLayer(pixelWidth, pixelHeight);
        }
    }
}
