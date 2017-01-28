using System;
using Avalonia.Media;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines a renderer used to render a visual brush to a bitmap.
    /// </summary>
    public interface IVisualBrushRenderer
    {
        /// <summary>
        /// Renders a visual brush to a bitmap.
        /// </summary>
        /// <param name="context">The drawing context to render to.</param>
        /// <param name="brush">The visual brush.</param>
        /// <returns>A bitmap containing the rendered brush.</returns>
        void RenderVisualBrush(IDrawingContextImpl context, VisualBrush brush);
    }
}
