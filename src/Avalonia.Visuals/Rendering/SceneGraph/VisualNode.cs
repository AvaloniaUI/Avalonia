// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the low-level scene graph representing an <see cref="IVisual"/>.
    /// </summary>
    public class VisualNode : IVisualNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualNode"/> class.
        /// </summary>
        /// <param name="visual">The visual that this node represents.</param>
        /// <param name="parent">The parent scene graph node, if any.</param>
        public VisualNode(IVisual visual, IVisualNode parent)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            if (parent == null && visual.VisualParent != null)
            {
                throw new AvaloniaInternalException(
                    "Attempted to create root VisualNode for parented visual.");
            }

            Visual = visual;
            Parent = parent;
            Children = new List<ISceneNode>();
        }

        /// <inheritdoc/>
        public IVisual Visual { get; }

        /// <inheritdoc/>
        public IVisualNode Parent { get; }

        /// <inheritdoc/>
        public Matrix Transform { get; set; }

        /// <inheritdoc/>
        public Rect ClipBounds { get; set; }

        /// <inheritdoc/>
        public bool ClipToBounds { get; set; }

        /// <inheritdoc/>
        public Geometry GeometryClip { get; set; }

        /// <summary>
        /// Gets or sets the opacity of the scnee graph node.
        /// </summary>
        public double Opacity { get; set; }

        /// <summary>
        /// Gets or sets the opacity mask for the scnee graph node.
        /// </summary>
        public IBrush OpacityMask { get; set; }

        /// <summary>
        /// Gets the child scene graph nodes.
        /// </summary>
        public List<ISceneNode> Children { get; }

        /// <summary>
        /// Gets a value indicating whether this node in the scene graph has already
        /// been updated in the current update pass.
        /// </summary>
        public bool SubTreeUpdated { get; set; }

        /// <inheritdoc/>
        IReadOnlyList<ISceneNode> IVisualNode.Children => Children;

        /// <summary>
        /// Makes a copy of the node
        /// </summary>
        /// <param name="parent">The new parent node.</param>
        /// <returns>A cloned node.</returns>
        public VisualNode Clone(IVisualNode parent)
        {
            return new VisualNode(Visual, parent)
            {
                Transform = Transform,
                ClipBounds = ClipBounds,
                ClipToBounds = ClipToBounds,
                GeometryClip = GeometryClip,
                Opacity = Opacity,
                OpacityMask = OpacityMask,
            };
        }

        /// <inheritdoc/>
        public bool HitTest(Point p)
        {
            foreach (var child in Children)
            {
                var geometry = child as IGeometryNode;

                if (geometry?.HitTest(p) == true)
                {
                    return true;
                }
            }

            return false;
        }

        public void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;

            if (Opacity != 1)
            {
                context.PushOpacity(Opacity);
            }

            if (ClipToBounds)
            {
                context.PushClip(ClipBounds * Transform.Invert());
            }

            foreach (var child in Children)
            {
                child.Render(context);
            }

            if (ClipToBounds)
            {
                context.PopClip();
            }

            if (Opacity != 1)
            {
                context.PopOpacity();
            }
        }
    }
}
