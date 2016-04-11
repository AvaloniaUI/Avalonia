// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Perspex.Media;

namespace Perspex.Controls.Shapes
{
    public class Polyline: Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            PerspexProperty.Register<Polyline, IList<Point>>("Points");

        static Polyline()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Polyline>(1);
            AffectsGeometry<Polyline>(PointsProperty);
        }

        public IList<Point> Points
        {
            get { return GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return new PolylineGeometry(Points, false);
        }
    }
}
