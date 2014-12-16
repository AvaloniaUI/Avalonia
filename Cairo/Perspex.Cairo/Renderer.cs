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
    using Perspex.Platform;
    using Matrix = Perspex.Matrix;

    /// <summary>
    /// A cairo renderer.
    /// </summary>
    public class Renderer : IRenderer
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
        /// Gets the number of times <see cref="Render"/> has been called.
        /// </summary>
        public int RenderCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="handle">A handle to the drawable.</param>
        public void Render(IVisual visual, IPlatformHandle handle)
        {
            using (DrawingContext context = CreateContext(handle))
            {
                this.Render(visual, context);
            }

            ++this.RenderCount;
        }

        /// <summary>
        /// Resizes the renderer.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void Resize(int width, int height)
        {
            // Don't need to do anything here.
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        /// <summary>
        /// Creates a cairo surface that targets a platform-specific resource.
        /// </summary>
        /// <param name="handle">The platform-specific handle.</param>
        /// <returns>A surface.</returns>
        private static DrawingContext CreateContext(IPlatformHandle handle)
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

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        private void Render(IVisual visual, DrawingContext context)
        {
            if (visual.IsVisible && visual.Opacity > 0)
            {
                Matrix transform = Matrix.Identity;

                if (visual.RenderTransform != null)
                {
                    Matrix current = context.CurrentTransform;
                    Matrix offset = Matrix.Translation(visual.TransformOrigin.ToPixels(visual.Bounds.Size));
                    transform = -current * -offset * visual.RenderTransform.Value * offset * current;
                }

                transform *= Matrix.Translation(visual.Bounds.Position);

                using (context.PushClip(visual.Bounds))
                using (context.PushTransform(transform))
                {
                    visual.Render(context);

                    foreach (var child in visual.VisualChildren)
                    {
                        this.Render(child, context);
                    }
                }
            }
        }
    }
}
