// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;

namespace Perspex.Media
{
    /// <summary>
    /// Represents the geometry of a line.
    /// </summary>
    public class LineGeometry : Geometry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineGeometry"/> class.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        public LineGeometry(Point startPoint, Point endPoint)
        {
            IPlatformRenderInterface factory = PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl context = impl.Open())
            {
                context.BeginFigure(startPoint, true);
                context.LineTo(endPoint);
                context.EndFigure(true);
            }

            PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Rect Bounds => PlatformImpl.Bounds;

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new LineGeometry(Bounds.TopLeft, Bounds.BottomRight);
        }
    }
}
