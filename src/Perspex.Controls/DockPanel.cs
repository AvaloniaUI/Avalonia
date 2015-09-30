namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Layout;

    public class DockPanel : Panel
    {
        public static readonly PerspexProperty<Dock> DockProperty = PerspexProperty.RegisterAttached<DockPanel, Control, Dock>("Dock");

        static DockPanel()
        {
            AffectsArrange(DockProperty);
        }

        // ReSharper disable once UnusedMember.Global
        public static Dock GetDock(PerspexObject perspexObject)
        {
            return perspexObject.GetValue(DockProperty);
        }

        // ReSharper disable once UnusedMember.Global
        public static void SetDock(PerspexObject element, Dock dock)
        {
            element.SetValue(DockProperty, dock);
        }

        public static readonly PerspexProperty<bool> LastChildFillProperty = PerspexProperty.Register<DockPanel, bool>(nameof(LastChildFillProperty), defaultValue: true);

        public bool LastChildFill
        {
            get { return GetValue(LastChildFillProperty); }
            set { SetValue(LastChildFillProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!LastChildFill)
            {
                return MeasureItemsThatWillBeDocked(availableSize, Children);
            }

            var sizeRequiredByDockingItems = MeasureItemsThatWillBeDocked(availableSize, Children.WithoutLast());
            var elementThatWillFill = Children.Last();
            elementThatWillFill.Measure(availableSize - sizeRequiredByDockingItems);
            var finalSize = sizeRequiredByDockingItems.Inflate(new Thickness(elementThatWillFill.DesiredSize.Width, elementThatWillFill.DesiredSize.Height));
            return finalSize;
        }

        private static Size MeasureItemsThatWillBeDocked(Size availableSize, IEnumerable<IControl> children)
        {
            var requiredHorizontalLength = 0D;
            var requiredVerticalLength = 0D;

            foreach (var control in children)
            {
                control.Measure(availableSize);

                var dock = control.GetValue(DockProperty);
                if (IsHorizontal(dock))
                {
                    requiredHorizontalLength += control.DesiredSize.Width;
                }
                else
                {
                    requiredVerticalLength += control.DesiredSize.Height;
                }
            }

            return new Size(requiredHorizontalLength, requiredVerticalLength);
        }

        private static bool IsHorizontal(Dock dock)
        {
            return dock == Dock.Left || dock == Dock.Right;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!LastChildFill)
            {
                return ArrangeAllChildren(finalSize);
            }
            else
            {
                return ArrangeChildrenAndFillLastChild(finalSize);
            }
        }

        private Size ArrangeChildrenAndFillLastChild(Size finalSize)
        {
            var docker = new DockingArranger();
            var requiredSize = docker.ArrangeAndGetUsedSize(finalSize, Children.WithoutLast());
            ArrangeToFill(Children.Last(), finalSize, docker.UsedMargin);
            return requiredSize;
        }

        private Size ArrangeAllChildren(Size finalSize)
        {
            return new DockingArranger().ArrangeAndGetUsedSize(finalSize, Children);
        }

        private static void ArrangeToFill(ILayoutable layoutable, Size containerSize, Margin margin)
        {
            var containerRect = new Rect(new Point(0, 0), containerSize);
            var marginsCutout = margin.AsThickness();
            var withoutMargins = containerRect.Deflate(marginsCutout);

            layoutable.Arrange(withoutMargins);
        }

        private class DockingArranger
        {
            public Margin UsedMargin { get; private set; }

            public Size ArrangeAndGetUsedSize(Size availableSize, IEnumerable<IControl> children)
            {
                var leftArranger = new LeftDocker(availableSize);
                var rightArranger = new RightDocker(availableSize);
                var topArranger = new LeftDocker(availableSize.Swap());
                var bottomArranger = new RightDocker(availableSize.Swap());

                UsedMargin = new Margin();

                foreach (var control in children)
                {
                    Rect dockedRect;
                    var dock = control.GetValue(DockProperty);
                    switch (dock)
                    {
                        case Dock.Left:
                            dockedRect = leftArranger.GetDockedRect(control.DesiredSize, UsedMargin, control.GetAlignments());
                            break;

                        case Dock.Top:
                            UsedMargin.Swap();
                            dockedRect = topArranger.GetDockedRect(control.DesiredSize.Swap(), UsedMargin, control.GetAlignments().Swap()).Swap();
                            UsedMargin.Swap();
                            break;

                        case Dock.Right:
                            dockedRect = rightArranger.GetDockedRect(control.DesiredSize, UsedMargin, control.GetAlignments());
                            break;

                        case Dock.Bottom:
                            UsedMargin.Swap();
                            dockedRect = bottomArranger.GetDockedRect(control.DesiredSize.Swap(), UsedMargin, control.GetAlignments().Swap()).Swap();
                            UsedMargin.Swap();
                            break;

                        default:
                            throw new InvalidOperationException($"Invalid dock value {dock}");
                    }

                    control.Arrange(dockedRect);
                }

                return availableSize;
            }
        }

        private class LeftDocker : Docker
        {
            public LeftDocker(Size availableSize) : base(availableSize)
            {
            }

            public override Rect GetDockedRect(Size childSize, Margin margin, Alignments alignments)
            {
                var marginsCutout = margin.AsThickness();
                var availableRect = OriginalRect.Deflate(marginsCutout);
                var alignedRect = AlignToLeft(availableRect, childSize, alignments.Vertical);

                AccumulatedOffset += childSize.Width;
                margin.Horizontal = margin.Horizontal.Offset(childSize.Width, 0);

                return alignedRect;
            }

            private static Rect AlignToLeft(Rect availableRect, Size childSize, Alignment verticalAlignment)
            {
                return availableRect.AlignChild(childSize, Alignment.Start, verticalAlignment);
            }
        }

        private class RightDocker : Docker
        {
            public RightDocker(Size availableSize) : base(availableSize)
            {
            }

            public override Rect GetDockedRect(Size childSize, Margin margin, Alignments alignments)
            {
                var marginsCutout = margin.AsThickness();
                var withoutMargins = OriginalRect.Deflate(marginsCutout);
                var finalRect = withoutMargins.AlignChild(childSize, Alignment.End, alignments.Vertical);

                AccumulatedOffset += childSize.Width;
                margin.Horizontal = margin.Horizontal.Offset(0, childSize.Width);

                return finalRect;
            }
        }

        private abstract class Docker
        {
            protected Docker(Size availableSize)
            {
                OriginalRect = new Rect(new Point(0, 0), availableSize);
            }

            protected double AccumulatedOffset { get; set; }

            protected Rect OriginalRect { get; }

            public abstract Rect GetDockedRect(Size childSize, Margin margin, Alignments alignments);
        }
    }

    public class Margin
    {
        public Segment Horizontal { get; set; }
        public Segment Vertical { get; set; }
    }

    public enum Alignment
    {
        Stretch, Start, Middle, End,
    }

    public static class SegmentMixin
    {
        public static Segment AlignToStart(this Segment container, double length)
        {
            return new Segment(container.Start, container.Start + length);
        }

        public static Segment AlignToEnd(this Segment container, double length)
        {
            return new Segment(container.End - length, container.End);
        }

        public static Segment AlignToMiddle(this Segment container, double length)
        {
            var start = container.Start + (container.Length - length) / 2;
            return new Segment(start, start + length);
        }
    }

    public struct Alignments
    {
        private readonly Alignment _horizontal;
        private readonly Alignment _vertical;

        public Alignments(Alignment horizontal, Alignment vertical)
        {
            _horizontal = horizontal;
            _vertical = vertical;
        }

        public Alignment Horizontal => _horizontal;

        public Alignment Vertical => _vertical;
    }

    public static class CoordinateMixin
    {
        private static Point Swap(this Point p)
        {
            return new Point(p.Y, p.X);
        }

        public static Size Swap(this Size s)
        {
            return new Size(s.Height, s.Width);
        }

        public static Rect Swap(this Rect r)
        {
            return new Rect(r.Position.Swap(), r.Size.Swap());
        }

        public static Segment Offset(this Segment l, double startOffset, double endOffset)
        {
            return new Segment(l.Start + startOffset, l.End + endOffset);
        }

        public static void Swap(this Margin m)
        {
            var v = m.Vertical;
            m.Vertical = m.Horizontal;
            m.Horizontal = v;
        }

        public static Thickness AsThickness(this Margin margin)
        {
            return new Thickness(margin.Horizontal.Start, margin.Vertical.Start, margin.Horizontal.End, margin.Vertical.End);
        }

        private static Alignment AsAlignment(this HorizontalAlignment horz)
        {
            switch (horz)
            {
                case HorizontalAlignment.Stretch:
                    return Alignment.Stretch;
                case HorizontalAlignment.Left:
                    return Alignment.Start;
                case HorizontalAlignment.Center:
                    return Alignment.Middle;
                case HorizontalAlignment.Right:
                    return Alignment.End;
                default:
                    throw new ArgumentOutOfRangeException(nameof(horz), horz, null);
            }
        }

        private static Alignment AsAlignment(this VerticalAlignment vert)
        {
            switch (vert)
            {
                case VerticalAlignment.Stretch:
                    return Alignment.Stretch;
                case VerticalAlignment.Top:
                    return Alignment.Start;
                case VerticalAlignment.Center:
                    return Alignment.Middle;
                case VerticalAlignment.Bottom:
                    return Alignment.End;
                default:
                    throw new ArgumentOutOfRangeException(nameof(vert), vert, null);
            }
        }

        public static Alignments GetAlignments(this ILayoutable layoutable)
        {
            return new Alignments(layoutable.HorizontalAlignment.AsAlignment(), layoutable.VerticalAlignment.AsAlignment());
        }

        public static Alignments Swap(this Alignments alignments)
        {
            return new Alignments(alignments.Vertical, alignments.Horizontal);
        }
    }

    public enum Dock
    {
        Left = 0,
        Bottom,
        Right,
        Top
    }

    public static class RectMixin
    {
        public static Rect AlignChild(this Rect container, Size childSize, Alignment horizontalAlignment, Alignment verticalAlignment)
        {
            var horzSegment = container.GetHorizontalCoordinates();
            var vertSegment = container.GetVerticalCoordinates();

            var horzResult = GetAlignedSegment(childSize.Width, horizontalAlignment, horzSegment);
            var vertResult = GetAlignedSegment(childSize.Height, verticalAlignment, vertSegment);

            return FromSegments(horzResult, vertResult);
        }

        private static Rect FromSegments(Segment horzSegment, Segment vertSegment)
        {
            return new Rect(horzSegment.Start, vertSegment.Start, horzSegment.Length, vertSegment.Length);
        }

        private static Segment GetAlignedSegment(double width, Alignment alignment, Segment horzSegment)
        {
            switch (alignment)
            {
                case Alignment.Start:
                    return horzSegment.AlignToStart(width);

                case Alignment.Middle:
                    return horzSegment.AlignToMiddle(width);

                case Alignment.End:
                    return horzSegment.AlignToEnd(width);

                default:
                    return new Segment(horzSegment.Start, horzSegment.End);
            }
        }

        private static Segment GetHorizontalCoordinates(this Rect rect)
        {
            return new Segment(rect.X, rect.Right);
        }

        private static Segment GetVerticalCoordinates(this Rect rect)
        {
            return new Segment(rect.Y, rect.Bottom);
        }
    }

    public struct Segment
    {
        public Segment(double start, double end)
        {
            Start = start;
            End = end;
        }

        public double Start { get; }
        public double End { get; }

        public double Length => End - Start;

        public override string ToString()
        {
            return $"Start: {Start}, End: {End}";
        }
    }

    public static class EnumerableMixin
    {
        private static IEnumerable<T> Shrink<T>(this IEnumerable<T> source, int left, int right)
        {
            int i = 0;
            var buffer = new Queue<T>(right + 1);

            foreach (T x in source)
            {
                if (i >= left) // Read past left many elements at the start
                {
                    buffer.Enqueue(x);
                    if (buffer.Count > right) // Build a buffer to drop right many elements at the end
                        yield return buffer.Dequeue();
                }
                else i++;
            }
        }
        public static IEnumerable<T> WithoutLast<T>(this IEnumerable<T> source, int n = 1)
        {
            return source.Shrink(0, n);
        }
        public static IEnumerable<T> WithoutFirst<T>(this IEnumerable<T> source, int n = 1)
        {
            return source.Shrink(n, 0);
        }
    }
}