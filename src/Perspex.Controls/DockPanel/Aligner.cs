namespace Perspex.Controls
{
    public static class LinearMarginMixin
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
}