// -----------------------------------------------------------------------
// <copyright file="Renderer.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using System;
    using Perspex.Direct2D1.Media;
    using Perspex.Platform;
    using SharpDX;
    using SharpDX.Direct2D1;
    using Splat;
    using DwFactory = SharpDX.DirectWrite.Factory;
    using Matrix = Perspex.Media.Matrix;

    /// <summary>
    /// Renders a <see cref="Canvas"/>.
    /// </summary>
    public class Renderer : IRenderer
    {
        /// <summary>
        /// The render target.
        /// </summary>
        private WindowRenderTarget renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        public void Initialize(IntPtr hwnd, double width, double height)
        {
            if (this.renderTarget != null)
            {
                throw new InvalidOperationException("Cannot initialize Renderer more than once.");
            }

            this.Direct2DFactory = Locator.Current.GetService<Factory>();
            this.DirectWriteFactory = Locator.Current.GetService<DwFactory>();

            RenderTargetProperties renderTargetProperties = new RenderTargetProperties
            {
            };

            HwndRenderTargetProperties hwndProperties = new HwndRenderTargetProperties
            {
                Hwnd = hwnd,
                PixelSize = new Size2((int)width, (int)height),
            };

            this.renderTarget = new WindowRenderTarget(
                this.Direct2DFactory,
                renderTargetProperties,
                hwndProperties);
        }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public Factory Direct2DFactory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public DwFactory DirectWriteFactory
        {
            get;
            private set;
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        public void Render(IVisual visual)
        {
            using (DrawingContext context = new DrawingContext(this.renderTarget, this.DirectWriteFactory))
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
            this.renderTarget.Resize(new Size2(width, height));
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        private void Render(IVisual visual, DrawingContext context)
        {
            visual.Render(context);

            foreach (Visual child in visual.VisualChildren)
            {
                Matrix translate = Matrix.Translation(child.Bounds.X, child.Bounds.Y);

                using (context.PushTransform(translate))
                {
                    this.Render(child, context);
                }
            }
        }
    }
}
