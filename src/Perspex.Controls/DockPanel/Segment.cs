namespace Perspex.Controls
{
    public struct Segment
    {
        public Segment(double start, double end)
        {
            Start = start;
            End = end;
        }

        public double Start { get; set; }
        public double End { get; set; }

        public double Length => End - Start;

        public override string ToString()
        {
            return $"Start: {Start}, End: {End}";
        }
    }
}