// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.EllipseGeometry"/>.
    /// </summary>
    public class EllipseGeometryImpl : GeometryImpl, IEllipseGeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public EllipseGeometryImpl(Rect rect)
            : base(CreateGeometry(rect))
        {
        }

        private static Geometry CreateGeometry(Rect rect)
        {
            var factory = AvaloniaLocator.Current.GetService<Factory>();
            var ellipse = new Ellipse(rect.Center.ToSharpDX(), (float)rect.Width / 2, (float)rect.Height / 2);
            return new EllipseGeometry(factory, ellipse);
        }
    }
}
