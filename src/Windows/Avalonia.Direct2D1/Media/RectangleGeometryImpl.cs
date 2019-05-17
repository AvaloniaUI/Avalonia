// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.RectangleGeometry"/>.
    /// </summary>
    internal class RectangleGeometryImpl : GeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public RectangleGeometryImpl(Rect rect)
            : base(CreateGeometry(rect))
        {
        }

        private static Geometry CreateGeometry(Rect rect)
        {
            return new RectangleGeometry(Direct2D1Platform.Direct2D1Factory, rect.ToDirect2D());
        }
    }
}
