





namespace Perspex.Input.Raw
{
    using System;

    public class RawSizeEventArgs : EventArgs
    {
        public RawSizeEventArgs(Size size)
        {
            this.Size = size;
        }

        public RawSizeEventArgs(double width, double height)
        {
            this.Size = new Size(width, height);
        }

        public Size Size { get; private set; }
    }
}
