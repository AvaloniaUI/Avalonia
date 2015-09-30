// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Perspex.Cairo.Media;
using Perspex.Media;
using Perspex.Platform;
using Perspex.Rendering;

namespace Perspex.Cairo
{
    using global::Cairo;

    /// <summary>
    /// A cairo renderer.
    /// </summary>
    public class RenderTarget : IRenderTarget
    {
        private readonly IPlatformHandle _handle;
        private readonly Surface _surface;
        private Gdk.Window _window;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="handle">The window handle.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public RenderTarget(IPlatformHandle handle, double width, double height)
        {
            _handle = handle;
        }

        public RenderTarget(ImageSurface surface)
        {
            _surface = surface;
        }

        /// <summary>
        /// Resizes the renderer.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public  void Resize(int width, int height)
        {
            // Don't need to do anything here.
        }


        /// <summary>
        /// Creates a cairo surface that targets a platform-specific resource.
        /// </summary>
        /// <param name="handle">The platform-specific handle.</param>
        /// <returns>A surface wrapped in an <see cref="IDrawingContext"/>.</returns>
        public IDrawingContext CreateDrawingContext()
        {
            if(_surface != null)
                return new DrawingContext(_surface);

            switch (_handle.HandleDescriptor)
            {
                case "GdkWindow":
                    if (_window == null)
                        _window = new Gdk.Window(_handle.Handle);

                    return new DrawingContext(_window);
                default:
                    throw new NotSupportedException(string.Format(
                        "Don't know how to create a Cairo renderer from a '{0}' handle",
                        _handle.HandleDescriptor));
            }
        }
        
        public void Dispose()
        {
			if (_surface != null)
		        _surface.Dispose();
        }
    }
}
