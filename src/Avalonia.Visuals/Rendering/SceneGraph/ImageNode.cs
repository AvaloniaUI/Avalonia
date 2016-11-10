// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.SceneGraph
{
    public class ImageNode : IGeometryNode
    {
        public ImageNode(Matrix transform, IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
        {
            Transform = transform;
            Source = source;
            Opacity = opacity;
            SourceRect = sourceRect;
            DestRect = destRect;
        }

        public Matrix Transform { get; }
        public IBitmapImpl Source { get; }
        public double Opacity { get; }
        public Rect SourceRect { get; }
        public Rect DestRect { get; }

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

        public bool HitTest(Point p)
        {
            return (DestRect * Transform).Contains(p);
        }
    }
}
