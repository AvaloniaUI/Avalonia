// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    internal class ImageNode : IDrawOperation
    {
        public ImageNode(Matrix transform, IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
        {
            Bounds = destRect.TransformToAABB(transform);
            Transform = transform;
            Source = source;
            Opacity = opacity;
            SourceRect = sourceRect;
            DestRect = destRect;
        }

        public Rect Bounds { get; }
        public Matrix Transform { get; }
        public IBitmapImpl Source { get; }
        public double Opacity { get; }
        public Rect SourceRect { get; }
        public Rect DestRect { get; }
        public IDictionary<VisualBrush, Scene> ChildScenes => null;

        public bool Equals(Matrix transform, IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
        {
            return transform == Transform &&
                Equals(source, Source) &&
                opacity == Opacity &&
                sourceRect == SourceRect &&
                destRect == DestRect;
        }

        public void Render(IDrawingContextImpl context)
        {
            context.Transform = Transform;
            context.DrawImage(Source, Opacity, SourceRect, DestRect);
        }

        public bool HitTest(Point p) => Bounds.Contains(p);
    }
}
