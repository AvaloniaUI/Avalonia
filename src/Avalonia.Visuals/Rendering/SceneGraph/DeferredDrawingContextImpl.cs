// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class DeferredDrawingContextImpl : IDrawingContextImpl
    {
        private Stack<Frame> _stack = new Stack<Frame>();

        public Matrix Transform { get; set; }

        private VisualNode Node => _stack.Peek().Node;

        private int Index
        {
            get { return _stack.Peek().Index; }
            set { _stack.Peek().Index = value; }
        }

        public IDisposable Begin(VisualNode node)
        {
            if (_stack.Count > 0)
            {
                var next = NextNodeAs<VisualNode>();

                if (next == null || next != node)
                {
                    Add(node);
                }
                else
                {
                    ++Index;
                }
            }

            _stack.Push(new Frame(node));
            return Disposable.Create(Pop);
        }

        public void Dispose()
        {
        }

        public void TrimNodes()
        {
            var frame = _stack.Peek();
            var children = frame.Node.Children;
            var index = frame.Index;

            if (children.Count > index)
            {
                children.RemoveRange(index, children.Count - index);
            }
        }

        public void DrawGeometry(IBrush brush, Pen pen, IGeometryImpl geometry)
        {
            var next = NextNodeAs<GeometryNode>();

            if (next == null || !next.Equals(Transform, brush, pen, geometry))
            {
                Add(new GeometryNode(Transform, brush, pen, geometry));
            }
            else
            {
                ++Index;
            }
        }

        public void DrawImage(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
        {
            var next = NextNodeAs<ImageNode>();

            if (next == null || !next.Equals(Transform, source, opacity, sourceRect, destRect))
            {
                Add(new ImageNode(Transform, source, opacity, sourceRect, destRect));
            }
            else
            {
                ++Index;
            }
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            var next = NextNodeAs<LineNode>();

            if (next == null || !next.Equals(Transform, pen, p1, p2))
            {
                Add(new LineNode(Transform, pen, p1, p2));
            }
            else
            {
                ++Index;
            }
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            var next = NextNodeAs<RectangleNode>();

            if (next == null || !next.Equals(Transform, null, pen, rect, cornerRadius))
            {
                Add(new RectangleNode(Transform, null, pen, rect, cornerRadius));
            }
            else
            {
                ++Index;
            }
        }

        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            var next = NextNodeAs<TextNode>();

            if (next == null || !next.Equals(Transform, foreground, origin, text))
            {
                Add(new TextNode(Transform, foreground, origin, text));
            }
            else
            {
                ++Index;
            }
        }

        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius = 0)
        {
            var next = NextNodeAs<RectangleNode>();

            if (next == null || !next.Equals(Transform, brush, null, rect, cornerRadius))
            {
                Add(new RectangleNode(Transform, brush, null, rect, cornerRadius));
            }
            else
            {
                ++Index;
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

        private void Add(ISceneNode node)
        {
            var index = Index;

            if (index < Node.Children.Count)
            {
                Node.Children[index] = node;
            }
            else
            {
                Node.Children.Add(node);
            }

            ++Index;
        }

        private T NextNodeAs<T>() where T : class, ISceneNode
        {
            return Index < Node.Children.Count ? Node.Children[Index] as T : null;
        }

        private void Pop() => _stack.Pop();

        class Frame
        {
            public Frame(VisualNode node)
            {
                Node = node;
            }

            public VisualNode Node { get; }
            public int Index { get; set; }
        }
    }
}
