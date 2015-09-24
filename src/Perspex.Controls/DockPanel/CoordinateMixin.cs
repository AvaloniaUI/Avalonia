namespace Perspex.Controls
{
    using System;
    using Layout;

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

        public static void Swap(this Margins m)
        {
            var v = m.VerticalMargin;
            m.VerticalMargin = m.HorizontalMargin;
            m.HorizontalMargin = v;
        }


        public static Thickness AsThickness(this Margins margins)
        {
            return new Thickness(margins.HorizontalMargin.Start, margins.VerticalMargin.Start, margins.HorizontalMargin.End, margins.VerticalMargin.End);
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

        public static LayoutSizes GetLayoutSizes(this ILayoutable layoutable)
        {
            return new LayoutSizes(
                new Size(layoutable.Width, layoutable.Height),
                new Size(layoutable.MaxWidth, layoutable.MaxHeight),
                new Size(layoutable.MinWidth, layoutable.MinHeight));
        }

        public static LayoutSizes Swap(this LayoutSizes l)
        {
            return new LayoutSizes(l.Size.Swap(), l.MaxSize.Swap(), l.MinSize.Swap());
        }
    }
}