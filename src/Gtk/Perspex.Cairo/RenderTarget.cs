// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Perspex.Cairo.Media;
using Perspex.Media;
using Perspex.Platform;
using Perspex.Rendering;
using DrawingContext = Perspex.Media.DrawingContext;

namespace Perspex.Cairo
{
    using global::Cairo;

    /// <summary>
    /// A cairo render target.
    /// </summary>
    public class RenderTarget : IRenderTarget
    {
        private readonly Surface _surface;
        private Gtk.Window _window;

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


        /// <summary>
        /// Creates a cairo surface that targets a platform-specific resource.
        /// </summary>
        /// <returns>A surface wrapped in an <see cref="Perspex.Media.DrawingContext"/>.</returns>
        public DrawingContext CreateDrawingContext()
        {
            var ctx = _surface != null
                ? new Media.DrawingContext(_surface)
                : new Media.DrawingContext(_window.GdkWindow);
            return new DrawingContext(ctx);
        }
        
        public void Dispose() => _surface?.Dispose();
    }
}
