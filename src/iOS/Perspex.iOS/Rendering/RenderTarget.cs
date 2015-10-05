using Perspex.Media;
using Perspex.Platform;
using Perspex.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Perspex.iOS.Rendering
{
    /// <summary>
    /// iOS renderer.
    /// </summary>
    public class RenderTarget : IRenderTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="handle">The window handle.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public RenderTarget(IPlatformHandle handle, double width, double height)
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Creates an <see cref="IDrawingContext"/> for a rendering session.
        /// </summary>
        public Media.DrawingContext CreateDrawingContext()
        {
            return new Media.DrawingContext(new DrawingContext());
        }

        /// <summary>
        /// Resizes the rendered viewport.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void Resize(int width, int height)
        {
            // do we need to do anything here for iOS?
        }
    }
}
