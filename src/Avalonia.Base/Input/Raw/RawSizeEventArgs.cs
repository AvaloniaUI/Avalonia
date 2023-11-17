using System;
using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
    public class RawSizeEventArgs : EventArgs
    {
        public RawSizeEventArgs(Size size)
        {
            Size = size;
        }

        public RawSizeEventArgs(double width, double height)
        {
            Size = new Size(width, height);
        }

        public Size Size { get; private set; }
    }
}
