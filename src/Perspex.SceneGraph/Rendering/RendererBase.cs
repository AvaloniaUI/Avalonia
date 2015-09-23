// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Rendering
{
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

        public abstract void Dispose();

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="handle">An optional platform-specific handle.</param>
        public virtual void Render(IVisual visual, IPlatformHandle handle)
        {
            Render(visual, handle, Matrix.Identity);
            ++RenderCount;
        }

        /// <summary>
        /// Renders the specified visual with the specified transform and clip.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="handle">An optional platform-specific handle.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="clip">An optional clip rectangle.</param>
        public virtual void Render(IVisual visual, IPlatformHandle handle, Matrix transform, Rect? clip = null)
        {
            using (var context = CreateDrawingContext(handle))
            using (clip.HasValue ? context.PushClip(clip.Value) : null)
            {
                Render(visual, context, transform);
            }
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
        /// <param name="transform">The current transform.</param>
        protected virtual void Render(IVisual visual, IDrawingContext context,  Matrix transform)
        {
            var opacity = visual.Opacity;

            if (visual.IsVisible && opacity > 0)
            {
                // Translate any existing transform into this controls coordinate system.
                Matrix offset = Matrix.CreateTranslation(visual.Bounds.Position);
				transform = offset * transform;

                // Apply the control's render transform, if any.
                if (visual.RenderTransform != null)
                {
                    offset = Matrix.CreateTranslation(visual.TransformOrigin.ToPixels(visual.Bounds.Size));
                    transform *= -offset * visual.RenderTransform.Value * offset;
                }

                // Draw the control and its children.
				using (context.PushTransform (transform))
                using (context.PushOpacity(opacity))
                using (visual.ClipToBounds ? context.PushClip(visual.Bounds) : null)
                {
                    visual.Render(context);

                    foreach (var child in visual.VisualChildren.OrderBy(x => x.ZIndex))
                    {
						Render(child, context, transform.Invert() * offset);
                    }
                }
            }
        }
    }
}
