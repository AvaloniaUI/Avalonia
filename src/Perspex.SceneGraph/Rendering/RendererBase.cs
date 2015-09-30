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
    /// This class provides implements the platform-independent parts of <see cref="IRenderTarget"/>.
    /// </remarks>
    public static class RendererMixin
    {
        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="renderTarget">IRenderer instance</param>
        /// <param name="visual">The visual to render.</param>
        public static void Render(this IRenderTarget renderTarget, IVisual visual)
        {
            using (var ctx = renderTarget.CreateDrawingContext())
                ctx.Render(visual);
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="renderTarget">IRenderer instance</param>
        /// <param name="visual">The visual to render.</param>
        /// <param name="translation">The current translation.</param>
        /// <param name="transform">The current transform.</param>
        public static void Render(this IRenderTarget renderTarget, IVisual visual, Matrix translation, Matrix transform)
        {
            using (var ctx = renderTarget.CreateDrawingContext())
                ctx.Render(visual, translation, transform);
        }

        /// <summary>
        /// Renders the specified visual with the specified transform and clip.
        /// </summary>
        /// <param name="renderTarget">IRenderer instance</param>
        /// <param name="visual">The visual to render.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="clip">An optional clip rectangle.</param>
        public static void Render(this IRenderTarget renderTarget, IVisual visual, Matrix transform, Rect? clip = null)
        {
            using (var context = renderTarget.CreateDrawingContext())
                context.Render(visual, transform, clip);
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// 
        /// <param name="context">The drawing context.</param>
        public static void Render(this IDrawingContext context, IVisual visual)
        {
            context.Render(visual, Matrix.Identity);
        }

        /// <summary>
        /// Renders the specified visual with the specified transform and clip.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="clip">An optional clip rectangle.</param>
        public static void Render(this IDrawingContext context, IVisual visual, Matrix transform, Rect? clip = null)
        {
            using (clip.HasValue ? context.PushClip(clip.Value) : null)
            {
                context.Render(visual, Matrix.Identity, transform);
            }
        }
        
        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        /// <param name="translation">The current translation.</param>
        /// <param name="transform">The current transform.</param>
        public static void Render(this IDrawingContext context, IVisual visual, Matrix translation, Matrix transform)
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
                using (visual.ClipToBounds ? context.PushClip(new Rect(visual.Bounds.Size)) : null)
                {
                    visual.Render(context);
                    d.Dispose();

                    foreach (var child in visual.VisualChildren.OrderBy(x => x.ZIndex))
                    {
                        context.Render(child, translation, transform);
                    }
                }
            }
        }
    }
}
