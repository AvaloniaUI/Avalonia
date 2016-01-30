// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Perspex.Media;

namespace Perspex.Controls.Shapes
{
    public class Polygon : Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            PerspexProperty.Register<Polygon, IList<Point>>("Points");

        private Geometry _geometry;

        static Polygon()
        {
            PointsProperty.Changed.AddClassHandler<Polygon>(x => x.PointsChanged);
        }

        public override Geometry DefiningGeometry
        {
            get
            {
                if (_geometry == null)
                {
                    _geometry = new PolylineGeometry(Points, true);
                }

                return _geometry;
            }
        }

        public IList<Point> Points
        {
            get { return GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        private void PointsChanged(PerspexPropertyChangedEventArgs e)
        {
            _geometry = null;
            InvalidateMeasure();
        }
    }
}
