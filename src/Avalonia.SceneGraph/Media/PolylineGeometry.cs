// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an polyline or polygon.
    /// </summary>
    public class PolylineGeometry : Geometry
    {
        private IList<Point> _points;
        private bool _isFilled;

        public PolylineGeometry(IList<Point> points, bool isFilled)
        {
            _points = points;
            _isFilled = isFilled;
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            IStreamGeometryImpl impl = factory.CreateStreamGeometry();

            using (IStreamGeometryContextImpl context = impl.Open())
            {
                if (points.Count > 0)
                {
                    context.BeginFigure(points[0], isFilled);
                    for (int i = 1; i < points.Count; i++)
                    {
                        context.LineTo(points[i]);
                    }
                    context.EndFigure(isFilled);
                }
            }

            PlatformImpl = impl;
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new PolylineGeometry(new List<Point>(_points), _isFilled);
        }
    }
}
