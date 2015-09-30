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
        /// <param name="visual">The visual to render.</param>
        /// 
        /// <param name="context">The drawing context.</param>
        public static void Render(this IDrawingContext context, IVisual visual)
        {
            var opacity = visual.Opacity;
            if (visual.IsVisible && opacity > 0)
            {
                var m = Matrix.CreateTranslation(visual.Bounds.Position);

                var renderTransform = Matrix.Identity;

                if (visual.RenderTransform != null)
                {
                    var origin = visual.TransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                    var offset = Matrix.CreateTranslation(origin);
                    renderTransform = (-offset)*visual.RenderTransform.Value*(offset);
                }
                m = context.CurrentTransform.Invert()*renderTransform*m*context.CurrentTransform;

                using (context.PushTransform(m))
                using (context.PushOpacity(opacity))
                using (visual.ClipToBounds ? context.PushClip(new Rect(visual.Bounds.Size)) : null)
                {
                    visual.Render(context);
                    foreach (var child in visual.VisualChildren.OrderBy(x => x.ZIndex))
                    {
                        context.Render(child);
                    }
                }
            }
        }
    }
}
