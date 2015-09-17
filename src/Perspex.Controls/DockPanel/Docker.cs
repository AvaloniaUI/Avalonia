namespace Perspex.Controls
{
    public class Docker
    {
        private Size _availableSize;
        private double _accumulatedOffset;
        private Rect _originalRect;

        protected Docker(Size availableSize)
        {
            AvailableSize = availableSize;
            OriginalRect = new Rect(new Point(0, 0), AvailableSize);
        }

        protected Size AvailableSize
        {
            get { return _availableSize; }
            set { _availableSize = value; }
        }

        protected double AccumulatedOffset
        {
            get { return _accumulatedOffset; }
            set { _accumulatedOffset = value; }
        }

        protected Rect OriginalRect
        {
            get { return _originalRect; }
            set { _originalRect = value; }
        }
    }
}