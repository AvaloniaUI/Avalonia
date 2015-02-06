// -----------------------------------------------------------------------
// <copyright file="Renderer.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo
{
    using System;
    using System.Runtime.InteropServices;
    using global::Cairo;
    using Perspex.Cairo.Media;
    using Perspex.Media;
    using Perspex.Platform;
    using Perspex.Rendering;
    using Matrix = Perspex.Matrix;

    /// <summary>
    /// A cairo renderer.
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
            switch (handle.HandleDescriptor)
            {
                case "HWND":
                    return new DrawingContext(new Win32Surface(GetDC(handle.Handle)));
                case "HDC":
                    return new DrawingContext(new Win32Surface(handle.Handle));
                case "GdkWindow":
                    return new DrawingContext(new Gdk.Window(handle.Handle));
                default:
                    throw new NotSupportedException(string.Format(
                        "Don't know how to create a Cairo renderer from a '{0}' handle",
                        handle.HandleDescriptor));
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
    }
}
