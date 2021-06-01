using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Controls.Converters
{
    public class ProgressRingEndsToPathDataConverter : IMultiValueConverter
    {
        static readonly double TAU = 6.2831853071795862; //Math.Tau doesn't exist pre-.NET 5 :(
        static readonly double HALF_TAU = TAU / 2.0;
        static readonly double NO_PROGRESS = DegreesToRad(-90);
        
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            double angle0 = (double)values[2];
            
            double angle1 = (double)values[3];

            double startAngle;
            double endAngle;

            if (values.Count > 4) //we have min+max+value to work with
            {
                startAngle = NO_PROGRESS;

                double min = angle0;
                double max = angle1;
                
                double range = max - min;

                double value = (double)values[4];
                
                endAngle = DegreesToRad((((value - min) / range) * 360) - 90);

            }
            else //indeterminate
            {
                angle0 = DegreesToRad(angle0);
                angle1 = DegreesToRad(angle1);
                startAngle = Math.Min(angle0, angle1);
                endAngle = Math.Max(angle0, angle1);
            }

            double angleGap = RadToNormRad(endAngle - startAngle);

            Rect trackBounds = (Rect)values[0];

            double strokeInset = (double)values[1] / 2;

            double trackCenterX = (trackBounds.Width / 2);
            double trackCenterY = (trackBounds.Height / 2);
            double trackRadiusX = trackCenterX - strokeInset;
            double trackRadiusY = trackCenterY - strokeInset;

            double normStart = RadToNormRad(startAngle);
            double normEnd = RadToNormRad(endAngle);
            
            if ((normStart == normEnd) && (startAngle != endAngle))
            {
                return new EllipseGeometry()
                {
                    RadiusX = trackRadiusX,
                    RadiusY = trackRadiusY,
                    Center = new Point(trackCenterX, trackCenterY)
                };

                /*Point ringPoint = GetRingPoint(trackRadiusX, trackRadiusY, trackCenterX, trackCenterY, startAngle);
                return new PathGeometry()
                {
                    Figures = 
                    {
                        new PathFigure()
                        {
                            StartPoint = ringPoint,
                            Segments = 
                            {
                                new ArcSegment()
                                {
                                    Point = ringPoint,
                                    IsLargeArc = true,
                                    Size = new Size(trackRadiusX, trackRadiusY),
                                    SweepDirection = SweepDirection.Clockwise
                                }
                            },
                            IsClosed = true
                        }
                    }
                };*/
            }
            else
            {
                return new PathGeometry()
                {
                    Figures = 
                    {
                        new PathFigure()
                        {
                            StartPoint = GetRingPoint(trackRadiusX, trackRadiusY, trackCenterX, trackCenterY, startAngle),
                            Segments = 
                            {
                                new ArcSegment()
                                {
                                    Point = GetRingPoint(trackRadiusX, trackRadiusY, trackCenterX, trackCenterY, endAngle),
                                    IsLargeArc = angleGap >= HALF_TAU,
                                    Size = new Size(trackRadiusX, trackRadiusY),
                                    SweepDirection = SweepDirection.Clockwise
                                }
                            },
                            IsClosed = false
                        }
                    }
                };
            }
        }

        internal static double DegreesToRad(double inAngle) =>
            inAngle * Math.PI / 180;
        
        internal static double RadToNormRad(double inAngle) =>
            (0 + (inAngle % TAU) + TAU) % TAU;


        internal static Point GetRingPoint(double trackRadiusX, double trackRadiusY, double trackCenterX, double trackCenterY, double angle) =>
            new Point((trackRadiusX * Math.Cos(angle)) + trackCenterX, (trackRadiusY * Math.Sin(angle)) + trackCenterY);
    }
}