// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Media;

namespace Perspex.Controls.Shapes
{
    public class Line : Shape
    {
        public static readonly StyledProperty<Point> StartPointProperty =
            PerspexProperty.Register<Line, Point>("StartPoint");

        public static readonly StyledProperty<Point> EndPointProperty =
            PerspexProperty.Register<Line, Point>("EndPoint");

        static Line()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Line>(1);
            AffectsGeometry(StartPointProperty, EndPointProperty);
        }

        public Point StartPoint
        {
            get { return GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        public Point EndPoint
        {
            get { return GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return new LineGeometry(StartPoint, EndPoint);
        }
    }
}
