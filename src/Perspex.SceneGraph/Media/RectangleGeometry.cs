// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;

namespace Perspex.Media
{
    /// <summary>
    /// Represents the geometry of a rectangle.
    /// </summary>
    public class RectangleGeometry : Geometry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle bounds.</param>
        public RectangleGeometry(Rect rect)
        {
            IPlatformRenderInterface factory = PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl context = impl.Open())
            {
                context.BeginFigure(rect.TopLeft, true);
                context.LineTo(rect.TopRight);
                context.LineTo(rect.BottomRight);
                context.LineTo(rect.BottomLeft);
                context.EndFigure(true);
            }

            PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Rect Bounds => PlatformImpl.Bounds;

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new RectangleGeometry(Bounds);
        }
    }
}
