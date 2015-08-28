// -----------------------------------------------------------------------
// <copyright file="IVisual.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using Perspex.Collections;
    using Perspex.Media;
    using Perspex.VisualTree;

    /// <summary>
    /// Represents a node in the visual scene graph.
    /// </summary>
    /// <remarks>
    /// The <see cref="IVisual"/> interface defines the interface required for a renderer to
    /// render a scene graph. You should not usually need to reference this interface unless
    /// you are writing a renderer; instead use the extension methods defined in
    /// <see cref="VisualExtensions"/> to traverse the scene graph. This interface is
    /// implemented by <see cref="Visual"/>. It should not be necessary to implement it
    /// anywhere else.
    /// </remarks>
    public interface IVisual
    {
        /// <summary>
        /// Gets the bounds of the scene graph node.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Gets a value indicating whether the scene graph node should be clipped to its bounds.
        /// </summary>
        bool ClipToBounds { get; set; }

        /// <summary>
        /// Gets a value indicating whether this scene graph node is attached to a visual root.
        /// </summary>
        bool IsAttachedToVisualTree { get; }

        /// <summary>
        /// Gets a value indicating whether this scene graph node and all its parents are visible.
        /// </summary>
        bool IsEffectivelyVisible { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this scene graph node is visible.
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the opacity of the scene graph node.
        /// </summary>
        double Opacity { get; set; }

        /// <summary>
        /// Gets or sets the render transform of the scene graph node.
        /// </summary>
        Transform RenderTransform { get; set; }

        /// <summary>
        /// Gets or sets the transform origin of the scene graph node.
        /// </summary>
        Origin TransformOrigin { get; set; }

        /// <summary>
        /// Gets the scene graph node's child nodes.
        /// </summary>
        IPerspexReadOnlyList<IVisual> VisualChildren { get; }

        /// <summary>
        /// Gets the scene graph node's parent node.
        /// </summary>
        IVisual VisualParent { get; }

        /// <summary>
        /// Gets or sets the Z index of the node.
        /// </summary>
        int ZIndex { get; set; }

        /// <summary>
        /// Renders the scene graph node to a <see cref="IDrawingContext"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        void Render(IDrawingContext context);

        /// <summary>
        /// Returns a transform that transforms the visual's coordinates into the coordinates
        /// of the specified <paramref name="visual"/>.
        /// </summary>
        /// <param name="visual">The visual to translate the coordinates to.</param>
        /// <returns>A <see cref="Matrix"/> containing the transform.</returns>
        Matrix TransformToVisual(IVisual visual);
    }
}
