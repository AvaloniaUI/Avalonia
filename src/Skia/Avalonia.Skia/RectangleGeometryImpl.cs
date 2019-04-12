// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="Avalonia.Media.RectangleGeometry"/>.
    /// </summary>
    internal class RectangleGeometryImpl : GeometryImpl
    {
        public override Rect Bounds { get; }
        public override SKPath EffectivePath { get; }

        public RectangleGeometryImpl(Rect rect)
        {
            var path = new SKPath();
            path.AddRect(rect.ToSKRect());

            EffectivePath = path;
            Bounds = rect;
        }
    }
}
