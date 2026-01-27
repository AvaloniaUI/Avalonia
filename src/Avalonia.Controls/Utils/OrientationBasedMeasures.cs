namespace Avalonia.Controls.Utils;

internal enum ScrollOrientation
{
    Horizontal,
    Vertical
}

internal interface IOrientationBasedMeasures
{
    ScrollOrientation ScrollOrientation { get; }

    bool IsVertical => ScrollOrientation is ScrollOrientation.Vertical;
}

internal static class OrientationBasedMeasuresExt
{
    extension(IOrientationBasedMeasures m)
    {
        /// <summary>
        /// The length of non-scrolling direction
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public double Major(Size size) =>
            m.IsVertical ? size.Height : size.Width;

        /// <summary>
        /// The length of scrolling direction
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public double Minor(Size size) =>
            m.IsVertical ? size.Width : size.Height;

        public T Minor<T>(T width, T height) =>
            m.IsVertical ? width : height;

        public T Major<T>(T width, T height) =>
            m.IsVertical ? height : width;

        public void SetMajor(ref Size size, double value) =>
            size = m.IsVertical ? size.WithHeight(value) : size.WithWidth(value);

        public void SetMinor(ref Size size, double value) =>
            size = m.IsVertical ? size.WithWidth(value) : size.WithHeight(value);

        public double MajorSize(Rect rect) =>
            m.IsVertical ? rect.Height : rect.Width;

        public void SetMajorSize(ref Rect rect, double value) =>
            rect = m.IsVertical ? rect.WithHeight(value) : rect.WithWidth(value);

        public double MinorSize(Rect rect) =>
            m.IsVertical ? rect.Width : rect.Height;

        public void SetMinorSize(ref Rect rect, double value) =>
            rect = m.IsVertical ? rect.WithWidth(value) : rect.WithHeight(value);

        public double MajorStart(Rect rect) =>
            m.IsVertical ? rect.Y : rect.X;

        public double MajorEnd(Rect rect) =>
            m.IsVertical ? rect.Bottom : rect.Right;

        public double MinorStart(Rect rect) =>
            m.IsVertical ? rect.X : rect.Y;

        public void SetMinorStart(ref Rect rect, double value) =>
            rect = m.IsVertical ? rect.WithX(value) : rect.WithY(value);

        public void SetMajorStart(ref Rect rect, double value) =>
            rect = m.IsVertical ? rect.WithY(value) : rect.WithX(value);

        public double MinorEnd(Rect rect) =>
            m.IsVertical ? rect.Right : rect.Bottom;

        public Rect MinorMajorRect(double minor, double major, double minorSize, double majorSize) =>
            m.IsVertical ?
                new Rect(minor, major, minorSize, majorSize) :
                new Rect(major, minor, majorSize, minorSize);

        public Point MinorMajorPoint(double minor, double major) =>
            m.IsVertical ?
                new Point(minor, major) : new Point(major, minor);

        public Size MinorMajorSize(double minor, double major) =>
            m.IsVertical ?
                new Size(minor, major) : new Size(major, minor);
    }
}
