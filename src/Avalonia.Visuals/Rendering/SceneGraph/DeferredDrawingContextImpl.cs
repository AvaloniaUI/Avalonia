// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal class DeferredDrawingContextImpl : IDrawingContextImpl
    {
        private readonly ISceneBuilder _sceneBuilder;
        private VisualNode _node;
        private int _childIndex;
        private int _drawOperationindex;

        public DeferredDrawingContextImpl(ISceneBuilder sceneBuilder, SceneLayers layers)
        {
            _sceneBuilder = sceneBuilder;
            Layers = layers;
        }

        public Matrix Transform { get; set; } = Matrix.Identity;

        public SceneLayers Layers { get; }

        public UpdateState BeginUpdate(VisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            if (_node != null)
            {
                if (_childIndex < _node.Children.Count)
                {
                    _node.ReplaceChild(_childIndex, node);
                }
                else
                {
                    _node.AddChild(node);
                }

                ++_childIndex;
            }

            var state = new UpdateState(this, _node, _childIndex, _drawOperationindex);
            _node = node;
            _childIndex = _drawOperationindex = 0;
            return state;
        }

        public void Clear(Color color)
        {
            // Cannot clear a deferred scene.
        }

        public void Dispose()
        {
            // Nothing to do here as we allocate no unmanaged resources.
        }

        public void TrimChildren()
        {
            _node.TrimChildren(_childIndex);
        }

        public void DrawGeometry(IBrush brush, Pen pen, IGeometryImpl geometry)
        {
            var next = NextDrawAs<GeometryNode>();

            if (next == null || !next.Equals(Transform, brush, pen, geometry))
            {
                Add(new GeometryNode(Transform, brush, pen, geometry, CreateChildScene(brush)));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        public void DrawImage(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
        {
            var next = NextDrawAs<ImageNode>();

            if (next == null || !next.Equals(Transform, source, opacity, sourceRect, destRect))
            {
                Add(new ImageNode(Transform, source, opacity, sourceRect, destRect));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            var next = NextDrawAs<LineNode>();

            if (next == null || !next.Equals(Transform, pen, p1, p2))
            {
                Add(new LineNode(Transform, pen, p1, p2));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            var next = NextDrawAs<RectangleNode>();

            if (next == null || !next.Equals(Transform, null, pen, rect, cornerRadius))
            {
                Add(new RectangleNode(Transform, null, pen, rect, cornerRadius, CreateChildScene(pen.Brush)));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            var next = NextDrawAs<TextNode>();

            if (next == null || !next.Equals(Transform, foreground, origin, text))
            {
                Add(new TextNode(Transform, foreground, origin, text));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius = 0)
        {
            var next = NextDrawAs<RectangleNode>();

            if (next == null || !next.Equals(Transform, brush, null, rect, cornerRadius))
            {
                Add(new RectangleNode(Transform, brush, null, rect, cornerRadius, CreateChildScene(brush)));
            }
            else
            {
                ++_drawOperationindex;
            }
        }

        public void PopClip()
        {
            // TODO: Implement
        }

        public void PopGeometryClip()
        {
            // TODO: Implement
        }

        public void PopOpacity()
        {
            // TODO: Implement
        }

        public void PopOpacityMask()
        {
            // TODO: Implement
        }

        public void PushClip(Rect clip)
        {
            // TODO: Implement
        }

        public void PushGeometryClip(Geometry clip)
        {
            // TODO: Implement
        }

        public void PushOpacity(double opacity)
        {
            // TODO: Implement
        }

        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            // TODO: Implement
        }

        public struct UpdateState : IDisposable
        {
            public UpdateState(
                DeferredDrawingContextImpl owner,
                VisualNode node,
                int childIndex,
                int drawOperationIndex)
            {
                Owner = owner;
                Node = node;
                ChildIndex = childIndex;
                DrawOperationIndex = drawOperationIndex;
            }

            public void Dispose()
            {
                Owner._node.TrimDrawOperations(Owner._drawOperationindex);

                var dirty = Owner.Layers.GetOrAdd(Owner._node.LayerRoot).Dirty;

                foreach (var operation in Owner._node.DrawOperations)
                {
                    dirty.Add(operation.Bounds);
                }

                Owner._node = Node;
                Owner._childIndex = ChildIndex;
                Owner._drawOperationindex = DrawOperationIndex;
            }

            public DeferredDrawingContextImpl Owner { get; }
            public VisualNode Node { get; }
            public int ChildIndex { get; }
            public int DrawOperationIndex { get; }
        }

        private void Add(IDrawOperation  node)
        {
            if (_drawOperationindex < _node.DrawOperations.Count)
            {
                _node.ReplaceDrawOperation(_drawOperationindex, node);
            }
            else
            {
                _node.AddDrawOperation(node);
            }

            ++_drawOperationindex;
        }

        private T NextDrawAs<T>() where T : class, IDrawOperation
        {
            return _drawOperationindex < _node.DrawOperations.Count ? _node.DrawOperations[_drawOperationindex] as T : null;
        }

        private IDictionary<IVisual, Scene> CreateChildScene(IBrush brush)
        {
            var visualBrush = brush as VisualBrush;

            if (visualBrush != null)
            {
                var visual = visualBrush.Visual;

                if (visual != null)
                {
                    (visual as IVisualBrushInitialize)?.EnsureInitialized();
                    var scene = new Scene(visual);
                    _sceneBuilder.UpdateAll(scene);
                    return new Dictionary<IVisual, Scene> { { visualBrush.Visual, scene } };
                }
            }

            return null;
        }
    }
}
