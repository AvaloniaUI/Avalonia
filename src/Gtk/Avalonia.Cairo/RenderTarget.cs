// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Avalonia.Cairo.Media;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Gtk;
using DrawingContext = Avalonia.Media.DrawingContext;

namespace Avalonia.Cairo
{
    using Avalonia.Cairo.Media.Imaging;
    using global::Cairo;

    /// <summary>
    /// A cairo render target.
    /// </summary>
    public class RenderTarget : IRenderTarget
    {
        private readonly Surface _surface;
        private readonly Func<Gdk.Drawable> _drawableAccessor;


        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public RenderTarget(Func<Gdk.Drawable> drawable)
        {
            _drawableAccessor = drawable;
        }

        public RenderTarget(ImageSurface surface)
        {
            _surface = surface;
        }

        /// <summary>
        /// Creates a cairo surface that targets a platform-specific resource.
        /// </summary>
        /// <param name="visualBrushRenderer">The visual brush renderer to use.</param>
        /// <returns>A surface wrapped in an <see cref="Avalonia.Media.DrawingContext"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            if (_drawableAccessor != null)
                return new Media.DrawingContext(_drawableAccessor(), visualBrushRenderer);
            if (_surface != null)
                return new Media.DrawingContext(_surface, visualBrushRenderer);
            throw new InvalidOperationException("Unspecified render target");
        }

        public IRenderTargetBitmapImpl CreateLayer(int pixelWidth, int pixelHeight)
        {
            return new RenderTargetBitmapImpl(new ImageSurface(Format.Argb32, pixelWidth, pixelHeight));
        }

        public void Dispose() => _surface?.Dispose();
    }
}
