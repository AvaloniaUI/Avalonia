// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the low-level scene graph representing an <see cref="IVisual"/>.
    /// </summary>
    internal class VisualNode : IVisualNode
    {
        private static readonly IReadOnlyList<IVisualNode> EmptyChildren = new IVisualNode[0];
        private static readonly IReadOnlyList<IDrawOperation> EmptyDrawOperations = new IDrawOperation[0];

        private Rect? _bounds;
        private double _opacity;
        private List<IVisualNode> _children;
        private List<IDrawOperation> _drawOperations;
        private bool _drawOperationsCloned;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualNode"/> class.
        /// </summary>
        /// <param name="visual">The visual that this node represents.</param>
        /// <param name="parent">The parent scene graph node, if any.</param>
        public VisualNode(IVisual visual, IVisualNode parent)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            Visual = visual;
            Parent = parent;
            HasAncestorGeometryClip = parent != null && 
                (parent.HasAncestorGeometryClip || parent.GeometryClip != null);
        }

        /// <inheritdoc/>
        public IVisual Visual { get; }

        /// <inheritdoc/>
        public IVisualNode Parent { get; }

        /// <inheritdoc/>
        public Matrix Transform { get; set; }

        /// <inheritdoc/>
        public Rect Bounds => _bounds ?? CalculateBounds();

        /// <inheritdoc/>
        public Rect ClipBounds { get; set; }

        /// <inheritdoc/>
        public bool ClipToBounds { get; set; }

        /// <inheritdoc/>
        public IGeometryImpl GeometryClip { get; set; }

        /// <inheritdoc/>
        public bool HasAncestorGeometryClip { get; }

        /// <summary>
        /// Gets or sets the opacity of the scene graph node.
        /// </summary>
        public double Opacity
        {
            get { return _opacity; }
            set
            {
                if (_opacity != value)
                {
                    _opacity = value;
                    OpacityChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the opacity mask for the scnee graph node.
        /// </summary>
        public IBrush OpacityMask { get; set; }

        /// <summary>
        /// Gets a value indicating whether this node in the scene graph has already
        /// been updated in the current update pass.
        /// </summary>
        public bool SubTreeUpdated { get; set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Opacity"/> property has changed.
        /// </summary>
        public bool OpacityChanged { get; private set; }

        public IVisual LayerRoot { get; set; }

        /// <inheritdoc/>
        public IReadOnlyList<IVisualNode> Children => _children ?? EmptyChildren;

        /// <inheritdoc/>
        public IReadOnlyList<IDrawOperation> DrawOperations => _drawOperations ?? EmptyDrawOperations;

        /// <summary>
        /// Adds a child to the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="child">The child to add.</param>
        public void AddChild(IVisualNode child)
        {
            EnsureChildrenCreated();
            _children.Add(child);
        }

        /// <summary>
        /// Adds an operation to the <see cref="DrawOperations"/> collection.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        public void AddDrawOperation(IDrawOperation operation)
        {
            EnsureDrawOperationsCreated();
            _drawOperations.Add(operation);
        }

        /// <summary>
        /// Removes a child from the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="child">The child to remove.</param>
        public void RemoveChild(IVisualNode child)
        {
            EnsureChildrenCreated();
            _children.Remove(child);
        }

        /// <summary>
        /// Replaces a child in the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="index">The child to be replaced.</param>
        /// <param name="node">The child to add.</param>
        public void ReplaceChild(int index, IVisualNode node)
        {
            EnsureChildrenCreated();
            _children[index] = node;
        }

        /// <summary>
        /// Replaces an item in the <see cref="DrawOperations"/> collection.
        /// </summary>
        /// <param name="index">The opeation to be replaced.</param>
        /// <param name="operation">The operation to add.</param>
        public void ReplaceDrawOperation(int index, IDrawOperation operation)
        {
            EnsureDrawOperationsCreated();
            _drawOperations[index] = operation;
        }

        /// <summary>
        /// Removes items in the <see cref="Children"/> collection from the specified index
        /// to the end.
        /// </summary>
        /// <param name="first">The index of the first child to be removed.</param>
        public void TrimChildren(int first)
        {
            if (first < _children?.Count)
            {
                EnsureChildrenCreated();
                _children.RemoveRange(first, _children.Count - first);
            }
        }

        /// <summary>
        /// Removes items in the <see cref="DrawOperations"/> collection from the specified index
        /// to the end.
        /// </summary>
        /// <param name="first">The index of the first operation to be removed.</param>
        public void TrimDrawOperations(int first)
        {
            if (first < _drawOperations?.Count)
            {
                EnsureDrawOperationsCreated();
                _drawOperations.RemoveRange(first, _drawOperations.Count - first);
            }
        }

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
                _opacity = Opacity,
                OpacityMask = OpacityMask,
                _drawOperations = _drawOperations,
                _drawOperationsCloned = true,
                LayerRoot= LayerRoot,
            };
        }

        /// <inheritdoc/>
        public bool HitTest(Point p)
        {
            foreach (var operation in DrawOperations)
            {
                if (operation.HitTest(p) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void BeginRender(IDrawingContextImpl context, bool skipOpacity)
        {
            if (ClipToBounds)
            {
                context.Transform = Matrix.Identity;
                context.PushClip(ClipBounds);
            }
            
            if (Opacity != 1 && !skipOpacity)
            {
                context.PushOpacity(Opacity);
            }

            context.Transform = Transform;

            if (GeometryClip != null)
            {
                context.PushGeometryClip(GeometryClip);
            }
        }

        /// <inheritdoc/>
        public void EndRender(IDrawingContextImpl context, bool skipOpacity)
        {
            if (GeometryClip != null)
            {
                context.PopGeometryClip();
            }

            if (Opacity != 1 && !skipOpacity)
            {
                context.PopOpacity();
            }

            if (ClipToBounds)
            {
                context.PopClip();
            }
        }

        private Rect CalculateBounds()
        {
            var result = new Rect();

            foreach (var operation in DrawOperations)
            {
                result = result.Union(operation.Bounds);
            }

            _bounds = result;
            return result;
        }

        private void EnsureChildrenCreated()
        {
            if (_children == null)
            {
                _children = new List<IVisualNode>();
            }
        }

        private void EnsureDrawOperationsCreated()
        {
            if (_drawOperations == null)
            {
                _drawOperations = new List<IDrawOperation>();
            }
            else if (_drawOperationsCloned)
            {
                _drawOperations = new List<IDrawOperation>(_drawOperations);
                _drawOperationsCloned = false;
            }
        }
    }
}
