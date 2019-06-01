namespace Avalonia.Controls.Repeaters
{
    internal class OrientationBasedMeasures
    {
        public Orientation ScrollOrientation { get; set; } = Orientation.Vertical;

        public double Major(in Size size) => ScrollOrientation == Orientation.Vertical ? size.Height : size.Width;
        public double Minor(in Size size) => ScrollOrientation == Orientation.Vertical ? size.Width : size.Height;
        public double MajorSize(in Rect rect) => ScrollOrientation == Orientation.Vertical ? rect.Height : rect.Width;
        public double MinorSize(in Rect rect) => ScrollOrientation == Orientation.Vertical ? rect.Width : rect.Height;
        public double MajorStart(in Rect rect) => ScrollOrientation == Orientation.Vertical ? rect.Y : rect.X;
        public double MinorStart(in Rect rect) => ScrollOrientation == Orientation.Vertical ? rect.X : rect.Y;
        public double MajorEnd(in Rect rect) => ScrollOrientation == Orientation.Vertical ? rect.Bottom : rect.Right;
        public double MinorEnd(in Rect rect) => ScrollOrientation == Orientation.Vertical ? rect.Right : rect.Bottom;

        public void SetMajorSize(ref Rect rect, double value)
        {
            if (ScrollOrientation == Orientation.Vertical)
            {
                rect = rect.WithHeight(value);
            }
            else
            {
                rect = rect.WithWidth(value);
            }
        }

        public void SetMinorSize(ref Rect rect, double value)
        {
            if (ScrollOrientation == Orientation.Vertical)
            {
                rect = rect.WithWidth(value);
            }
            else
            {
                rect = rect.WithHeight(value);
            }
        }

        public void SetMajorStart(ref Rect rect, double value)
        {
            if (ScrollOrientation == Orientation.Vertical)
            {
                rect = rect.WithY(value);
            }
            else
            {
                rect = rect.WithX(value);
            }
        }

        public void SetMinorStart(ref Rect rect, double value)
        {
            if (ScrollOrientation == Orientation.Vertical)
            {
                rect = rect.WithX(value);
            }
            else
            {
                rect = rect.WithY(value);
            }
        }

        public Rect MinorMajorRect(double minor, double major, double minorSize, double majorSize)
        {
            return ScrollOrientation == Orientation.Vertical ?
                new Rect(minor, major, minorSize, majorSize) :
                new Rect(major, minor, majorSize, minorSize);
        }

        public Point MinorMajorPoint(double minor, double major)
        {
            return ScrollOrientation == Orientation.Vertical ?
                new Point(minor, major) :
                new Point(major, minor);
        }

        public Size MinorMajorSize(double minor, double major)
        {
            return ScrollOrientation == Orientation.Vertical ?
                new Size(minor, major) :
                new Size(major, minor);
        }
    }
}
