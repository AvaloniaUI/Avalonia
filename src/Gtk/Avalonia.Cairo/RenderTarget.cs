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
    using global::Cairo;

    /// <summary>
    /// A cairo render target.
    /// </summary>
    public class RenderTarget : IRenderTarget
    {
        private readonly Surface _surface;
        private readonly Gtk.Window _window;
        private readonly Gtk.DrawingArea _area;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public RenderTarget(Gtk.Window window)
        {
            _window = window;
        }

        public RenderTarget(ImageSurface surface)
        {
            _surface = surface;
        }

        public RenderTarget(DrawingArea area)
        {
            _area = area;
        }

        /// <summary>
        /// Creates a cairo surface that targets a platform-specific resource.
        /// </summary>
        /// <param name="visualBrushRenderer">The visual brush renderer to use.</param>
        /// <returns>A surface wrapped in an <see cref="Avalonia.Media.DrawingContext"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            if (_window != null)
                return new Media.DrawingContext(_window.GdkWindow);
            if (_surface != null)
                return new Media.DrawingContext(_surface);
            if (_area != null)
                return new Media.DrawingContext(_area.GdkWindow);
            throw new InvalidOperationException("Unspecified render target");
        }

        public void Dispose() => _surface?.Dispose();
    }
}
