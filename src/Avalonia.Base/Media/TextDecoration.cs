using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a text decoration, which is a visual ornamentation that is added to text (such as an underline).
    /// </summary>
    public class TextDecoration : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Location"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationLocation> LocationProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationLocation>(nameof(Location));

        /// <summary>
        /// Defines the <see cref="Stroke"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> StrokeProperty =
            AvaloniaProperty.Register<TextDecoration, IBrush?>(nameof(Stroke));

        /// <summary>
        /// Defines the <see cref="StrokeThicknessUnit"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationUnit> StrokeThicknessUnitProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>(nameof(StrokeThicknessUnit));

        /// <summary>
        /// Defines the <see cref="StrokeDashArray"/> property.
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty =
            AvaloniaProperty.Register<TextDecoration, AvaloniaList<double>?>(nameof(StrokeDashArray));

        /// <summary>
        /// Defines the <see cref="StrokeDashOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeDashOffsetProperty =
            AvaloniaProperty.Register<TextDecoration, double>(nameof(StrokeDashOffset));

        /// <summary>
        /// Defines the <see cref="StrokeThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<TextDecoration, double>(nameof(StrokeThickness), 1);

        /// <summary>
        /// Defines the <see cref="StrokeLineCap"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
            AvaloniaProperty.Register<TextDecoration, PenLineCap>(nameof(StrokeLineCap));

        /// <summary>
        /// Defines the <see cref="StrokeOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeOffsetProperty =
            AvaloniaProperty.Register<TextDecoration, double>(nameof(StrokeOffset));

        /// <summary>
        /// Defines the <see cref="StrokeOffsetUnit"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationUnit> StrokeOffsetUnitProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>(nameof(StrokeOffsetUnit));

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public TextDecorationLocation Location
        {
            get => GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that specifies how the <see cref="TextDecoration"/> is painted.
        /// </summary>
        public IBrush? Stroke
        {
            get { return GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        /// <summary>
        /// Gets the units in which the thickness of the <see cref="TextDecoration"/> is expressed.
        /// </summary>
        public TextDecorationUnit StrokeThicknessUnit
        {
            get => GetValue(StrokeThicknessUnitProperty);
            set => SetValue(StrokeThicknessUnitProperty, value);
        }

        /// <summary>
        /// Gets or sets a collection of <see cref="double"/> values that indicate the pattern of dashes and gaps
        /// that is used to draw the <see cref="TextDecoration"/>.
        /// </summary>
        public AvaloniaList<double>? StrokeDashArray
        {
            get { return GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that specifies the distance within the dash pattern where a dash begins.
        /// </summary>
        public double StrokeDashOffset
        {
            get { return GetValue(StrokeDashOffsetProperty); }
            set { SetValue(StrokeDashOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the <see cref="TextDecoration"/>.
        /// </summary>
        public double StrokeThickness
        {
            get { return GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="PenLineCap"/> enumeration value that describes the shape at the ends of a line.
        /// </summary>
        public PenLineCap StrokeLineCap
        {
            get { return GetValue(StrokeLineCapProperty); }
            set { SetValue(StrokeLineCapProperty, value); }
        }

        /// <summary>
        /// The stroke's offset.
        /// </summary>
        /// <value>
        /// The pen offset.
        /// </value>
        public double StrokeOffset
        {
            get => GetValue(StrokeOffsetProperty);
            set => SetValue(StrokeOffsetProperty, value);
        }

        /// <summary>
        /// Gets the units in which the <see cref="StrokeOffset"/> value is expressed.
        /// </summary>
        public TextDecorationUnit StrokeOffsetUnit
        {
            get => GetValue(StrokeOffsetUnitProperty);
            set => SetValue(StrokeOffsetUnitProperty, value);
        }

        /// <summary>
        /// Draws the <see cref="TextDecoration"/> at given origin.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="glyphRun">The decorated run.</param>
        /// <param name="textMetrics">The font metrics of the decorated run.</param>
        /// <param name="defaultBrush">The default brush that is used to draw the decoration.</param>
        internal void Draw(DrawingContext drawingContext, GlyphRun glyphRun, TextMetrics textMetrics, IBrush defaultBrush)
        {
            var baselineOrigin = glyphRun.BaselineOrigin;
            var thickness = StrokeThickness;

            switch (StrokeThicknessUnit)
            {
                case TextDecorationUnit.FontRecommended:
                    switch (Location)
                    {
                        case TextDecorationLocation.Underline:
                            thickness = textMetrics.UnderlineThickness;
                            break;
                        case TextDecorationLocation.Strikethrough:
                            thickness = textMetrics.StrikethroughThickness;
                            break;
                    }

                    break;
                case TextDecorationUnit.FontRenderingEmSize:
                    thickness = textMetrics.FontRenderingEmSize * thickness;
                    break;
            }

            var origin = new Point();

            switch (Location)
            {
                case TextDecorationLocation.Baseline:
                    origin += glyphRun.BaselineOrigin;
                    break;
                case TextDecorationLocation.Strikethrough:
                    origin += new Point(baselineOrigin.X, baselineOrigin.Y + textMetrics.StrikethroughPosition);
                    break;
                case TextDecorationLocation.Underline:
                    origin += new Point(baselineOrigin.X, baselineOrigin.Y + textMetrics.UnderlinePosition);
                    break;
            }

            switch (StrokeOffsetUnit)
            {
                case TextDecorationUnit.FontRenderingEmSize:
                    origin += new Point(0, StrokeOffset * textMetrics.FontRenderingEmSize);
                    break;
                case TextDecorationUnit.Pixel:
                    origin += new Point(0, StrokeOffset);
                    break;
            }

            var pen = new Pen(Stroke ?? defaultBrush, thickness,
                new DashStyle(StrokeDashArray, StrokeDashOffset), StrokeLineCap);

            if (Location != TextDecorationLocation.Strikethrough)
            {
                var offsetY = glyphRun.BaselineOrigin.Y - origin.Y;

                var intersections = glyphRun.GetIntersections((float)(thickness * 0.5d - offsetY), (float)(thickness * 1.5d - offsetY));

                if (intersections.Count > 0)
                {
                    var last = baselineOrigin.X;
                    var finalPos = last + glyphRun.Bounds.Width;
                    var end = last;

                    var points = new List<double>();

                    //math is taken from chrome's source code.
                    for (var i = 0; i < intersections.Count; i += 2)
                    {
                        var start = intersections[i] - thickness;
                        end = intersections[i + 1] + thickness;
                        if (start > last && last + textMetrics.FontRenderingEmSize / 12 < start)
                        {
                            points.Add(last);
                            points.Add(start);
                        }
                        last = end;
                    }

                    if (end < finalPos)
                    {
                        points.Add(end);
                        points.Add(finalPos);
                    }

                    for (var i = 0; i < points.Count; i += 2)
                    {
                        var a = new Point(points[i], origin.Y);
                        var b = new Point(points[i + 1], origin.Y);

                        drawingContext.DrawLine(pen, a, b);
                    }

                    return;
                }
            }

            drawingContext.DrawLine(pen, origin, origin + new Point(glyphRun.Metrics.Width, 0));
        }
    }
}
