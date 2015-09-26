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
    public class Renderer : RendererBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="handle">The window handle.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public Renderer(IPlatformHandle handle, double width, double height)
        {
        }

        //public Renderer(ImageSurface surface)
        //{
        //    _surface = surface;
        //}

        /// <summary>
        /// Resizes the renderer.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public override void Resize(int width, int height)
        {
            // Don't need to do anything here.
        }


        /// <summary>
        /// Creates a cairo surface that targets a platform-specific resource.
        /// </summary>
        /// <param name="handle">The platform-specific handle.</param>
        /// <returns>A surface wrapped in an <see cref="IDrawingContext"/>.</returns>
        protected override IDrawingContext CreateDrawingContext(IPlatformHandle handle)
        {
            //switch (handle.HandleDescriptor)
            //{
            //    case "RTB":
            //        return new DrawingContext(_surface);
            //    case "GdkWindow":
            //        if (_window == null)
            //            _window = new Gdk.Window(handle.Handle);

            //        return new DrawingContext(_window);
            //    default:
            //        throw new NotSupportedException(string.Format(
            //            "Don't know how to create a Cairo renderer from a '{0}' handle",
            //            handle.HandleDescriptor));
            //}
            throw new NotImplementedException();
        }

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetDC(IntPtr hwnd);

        public override void Dispose()
        {
            //if (_surface != null)
            //    _surface.Dispose();
        }
    }
}
