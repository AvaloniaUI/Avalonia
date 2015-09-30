// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Direct2D1.Media;
using Perspex.Media;
using Perspex.Platform;
using Perspex.Rendering;
using SharpDX;
using SharpDX.Direct2D1;
using DwFactory = SharpDX.DirectWrite.Factory;

namespace Perspex.Direct2D1
{
    public class RenderTarget : IRenderTarget
    {
        /// <summary>
        /// The render target.
        /// </summary>
        private readonly SharpDX.Direct2D1.RenderTarget _renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public RenderTarget(IntPtr hwnd, double width, double height)
        {
            Direct2DFactory = PerspexLocator.Current.GetService<Factory>();
            DirectWriteFactory = PerspexLocator.Current.GetService<DwFactory>();

            RenderTargetProperties renderTargetProperties = new RenderTargetProperties
            {
            };

            HwndRenderTargetProperties hwndProperties = new HwndRenderTargetProperties
            {
                Hwnd = hwnd,
                PixelSize = new Size2((int)width, (int)height),
                PresentOptions = PresentOptions.Immediately,
            };

            _renderTarget = new WindowRenderTarget(
                Direct2DFactory,
                renderTargetProperties,
                hwndProperties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        public RenderTarget(SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            Direct2DFactory = PerspexLocator.Current.GetService<Factory>();
            DirectWriteFactory = PerspexLocator.Current.GetService<DwFactory>();
            _renderTarget = renderTarget;
        }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public Factory Direct2DFactory
        {
            get; }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public DwFactory DirectWriteFactory
        {
            get; }

        /// <summary>
        /// Resizes the renderer.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void Resize(int width, int height)
        {
            WindowRenderTarget window = _renderTarget as WindowRenderTarget;

            if (window == null)
            {
                throw new InvalidOperationException(string.Format(
                    "A renderer with a target of type '{0}' cannot be resized.",
                    _renderTarget.GetType().Name));
            }

            window.Resize(new Size2(width, height));
        }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="IDrawingContext"/>.</returns>
        public IDrawingContext CreateDrawingContext()
        {
            return new DrawingContext(_renderTarget, DirectWriteFactory);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }
    }
}
