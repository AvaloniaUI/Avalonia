// -----------------------------------------------------------------------
// <copyright file="Renderer.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo
{
    using System;
    using Perspex.Platform;
    using Splat;
    using global::Cairo;
    using System.Runtime.InteropServices;
    using Perspex.Cairo.Media;
    using Matrix = Perspex.Matrix;

    public class Renderer : IRenderer
    {
        /// <summary>
        /// The target cairo surface.
        /// </summary>
        private Surface surface;

        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public Renderer(IntPtr hwnd, double width, double height)
        {
            surface = new Win32Surface(GetDC(hwnd));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="surface">The target surface.</param>
        public Renderer(Surface surface)
        {
            this.surface = surface;
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        public void Render(IVisual visual)
        {
            using (DrawingContext context = new DrawingContext(this.surface))
            {
                this.Render(visual, context);
            }
        }

        /// <summary>
        /// Resizes the renderer.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void Resize(int width, int height)
        {
            var s = this.surface.CreateSimilar(Content.ColorAlpha, width, height);
            this.surface.Dispose();
            this.surface = s;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

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
