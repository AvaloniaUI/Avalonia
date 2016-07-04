// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Rendering;

namespace Avalonia.VisualTree
{
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
        /// Raised when the control is attached to a rooted visual tree.
        /// </summary>
        event EventHandler<VisualTreeAttachmentEventArgs> AttachedToVisualTree;

        /// <summary>
        /// Raised when the control is detached from a rooted visual tree.
        /// </summary>
        event EventHandler<VisualTreeAttachmentEventArgs> DetachedFromVisualTree;

        /// <summary>
        /// Gets the bounds of the scene graph node relative to its parent.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Gets a value indicating whether the scene graph node should be clipped to its bounds.
        /// </summary>
        bool ClipToBounds { get; set; }

        /// <summary>
        /// Gets or sets the geometry clip for this visual.
        /// </summary>
        Geometry Clip { get; set; }

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
        /// Gets or sets the opacity mask of the scene graph node.
        /// </summary>
        IBrush OpacityMask { get; set; }

        /// <summary>
        /// Gets or sets the render transform of the scene graph node.
        /// </summary>
        Transform RenderTransform { get; set; }

        /// <summary>
        /// Gets or sets the render transform origin of the scene graph node.
        /// </summary>
        RelativePoint RenderTransformOrigin { get; set; }

        /// <summary>
        /// Gets the scene graph node's child nodes.
        /// </summary>
        IAvaloniaReadOnlyList<IVisual> VisualChildren { get; }

        /// <summary>
        /// Gets the scene graph node's parent node.
        /// </summary>
        IVisual VisualParent { get; }

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        IRenderRoot VisualRoot { get; }

        /// <summary>
        /// Gets or sets the Z index of the node.
        /// </summary>
        int ZIndex { get; set; }

        /// <summary>
        /// Invalidates the visual and queues a repaint.
        /// </summary>
        void InvalidateVisual();

        /// <summary>
        /// Renders the scene graph node to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        void Render(DrawingContext context);

        /// <summary>
        /// Returns a transform that transforms the visual's coordinates into the coordinates
        /// of the specified <paramref name="visual"/>.
        /// </summary>
        /// <param name="visual">The visual to translate the coordinates to.</param>
        /// <returns>
        /// A <see cref="Matrix"/> containing the transform or null if the visuals don't share a
        /// common ancestor.
        /// </returns>
        Matrix? TransformToVisual(IVisual visual);
    }
}
