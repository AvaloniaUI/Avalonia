namespace Perspex.Controls
{
    using Layout;

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

        public static Rect FromSegments(Segment horzSegment, Segment vertSegment)
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

        public static Segment GetHorizontalCoordinates(this Rect rect)
        {
            return new Segment(rect.X, rect.Right);
        }

        public static Segment GetVerticalCoordinates(this Rect rect)
        {
            return new Segment(rect.Y, rect.Bottom);
        }
    }
}