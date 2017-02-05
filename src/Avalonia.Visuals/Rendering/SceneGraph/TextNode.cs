// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal class TextNode : IDrawOperation
    {
        public TextNode(Matrix transform, IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            Bounds = new Rect(origin, text.Size).TransformToAABB(transform);
            Transform = transform;
            Foreground = foreground;
            Origin = origin;
            Text = text;
        }

        public Rect Bounds { get; }
        public Matrix Transform { get; }
        public IBrush Foreground { get; }
        public Point Origin { get; }
        public IFormattedTextImpl Text { get; }
        public IDictionary<VisualBrush, Scene> ChildScenes => null;

        public void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawText(Foreground, Origin, Text);
        }

        internal bool Equals(Matrix transform, IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            return transform == Transform &&
                Equals(foreground, Foreground) &&
                origin == Origin &&
                Equals(text, Text);
        }

        public bool HitTest(Point p) => Bounds.Contains(p);
    }
}
