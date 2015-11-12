// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Android.Platform.CanvasPlatform;
using Perspex.Android.Platform.Specific;
using Perspex.Platform;
using AG = Android.Graphics;

namespace Perspex.Android.CanvasRendering
{
    /// <summary>
    /// A android render target.
    /// </summary>
    public class RenderTarget : IRenderTarget
    {
        private readonly AG.Bitmap _nativeBitmap;
        private IAndroidCanvasView _window;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public RenderTarget(IAndroidCanvasView window)
        {
            _window = window;
        }

        public RenderTarget(AG.Bitmap surface)
        {
            _nativeBitmap = surface;
        }

        /// <summary>
        /// Resizes the renderer.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void Resize(int width, int height)
        {
            if (_nativeBitmap != null)
            {
                this._nativeBitmap.Reconfigure(width, height, AG.Bitmap.Config.Argb8888);
            }
        }

        /// <summary>
        /// Creates a DrawingContext that targets a platform-specific resource.
        /// </summary>
        /// <returns>A surface wrapped in an <see cref="Perspex.Media.DrawingContext"/>.</returns>
        public Perspex.Media.DrawingContext CreateDrawingContext()
        {
            var canvas = _nativeBitmap != null ? new AG.Canvas(this._nativeBitmap) : _window.Canvas;
            return new Perspex.Media.DrawingContext(new DrawingContextImpl(canvas, _window != null ? _window.VisualCaches : null));
        }

        public void Dispose()
        {
            this._nativeBitmap?.Dispose();
        }
    }
}