// -----------------------------------------------------------------------
// <copyright file="RendererBase.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Rendering
{
    using System.Linq;
    using Perspex.Media;
    using Perspex.Platform;

    /// <summary>
    /// Base class for standard renderers.
    /// </summary>
    /// <remarks>
    /// This class provides implements the platform-independent parts of <see cref="IRenderer"/>.
    /// </remarks>
    public abstract class RendererBase : IRenderer
    {
        /// <summary>
        /// Gets the number of times <see cref="Render(IVisual, IPlatformHandle)"/> has been called.
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
        /// <param name="handle">An optional platform-specific handle.</param>
        public virtual void Render(IVisual visual, IPlatformHandle handle)
        {
            this.Render(visual, handle, Matrix.Identity);
        }

        /// <summary>
        /// Renders the specified visual with the specified transform.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="handle">An optional platform-specific handle.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="clip">An optional clip rectangle.</param>
        public virtual void Render(IVisual visual, IPlatformHandle handle, Matrix transform, Rect? clip = null)
        {
            using (var context = this.CreateDrawingContext(handle))
            using (clip.HasValue ? context.PushClip(clip.Value) : null)
            {
                this.Render(visual, context, Matrix.Identity, transform);
            }

            ++this.RenderCount;
        }

        /// <summary>
        /// Resizes the rendered viewport.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public abstract void Resize(int width, int height);

        /// <summary>
        /// When overriden by a derived class creates an <see cref="IDrawingContext"/> for a
        /// rendering session.
        /// </summary>
        /// <param name="handle">The handle to use to create the context.</param>
        /// <returns>An <see cref="IDrawingContext"/>.</returns>
        protected abstract IDrawingContext CreateDrawingContext(IPlatformHandle handle);

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        /// <param name="translation">The current translation.</param>
        /// <param name="transform">The current transform.</param>
        protected virtual void Render(IVisual visual, IDrawingContext context, Matrix translation, Matrix transform)
        {
            var opacity = visual.Opacity;

            if (visual.IsVisible && opacity > 0)
            {
                // Translate any existing transform into this controls coordinate system.
                Matrix offset = Matrix.CreateTranslation(visual.Bounds.Position);
                transform = offset * transform * -offset;

                // Update the current offset.
                translation *= Matrix.CreateTranslation(visual.Bounds.Position);

                // Apply the control's render transform, if any.
                if (visual.RenderTransform != null)
                {
                    offset = Matrix.CreateTranslation(visual.TransformOrigin.ToPixels(visual.Bounds.Size));
                    transform *= -offset * visual.RenderTransform.Value * offset;
                }

                // Draw the control and its children.
                var m = transform * translation;
                var d = context.PushTransform(m);

                using (context.PushOpacity(opacity))
                using (visual.ClipToBounds ? context.PushClip(visual.Bounds) : null)
                {
                    visual.Render(context);
                    d.Dispose();

                    foreach (var child in visual.VisualChildren.OrderBy(x => x.ZIndex))
                    {
                        this.Render(child, context, translation, transform);
                    }
                }
            }
        }
    }
}
