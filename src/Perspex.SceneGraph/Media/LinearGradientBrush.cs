// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Media
{
    public class LinearGradientBrush : GradientBrush
    {
        public static readonly PerspexProperty<Point> StartPointProperty =
PerspexProperty.Register<LinearGradientBrush, Point>(nameof(StartPoint), new Point(0, 0));

        public static readonly PerspexProperty<Point> EndPointProperty =
PerspexProperty.Register<LinearGradientBrush, Point>(nameof(EndPoint), new Point(0, 0));

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
    }
}