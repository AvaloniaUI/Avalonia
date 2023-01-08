using System;

namespace Avalonia.Win32.Interop.Wpf
{
    struct IntSize : IEquatable<IntSize>
    {
        public bool Equals(IntSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public IntSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public IntSize(double width, double height) : this((int) width, (int) height)
        {
            
        }

        public static implicit  operator IntSize(System.Windows.Size size)
        {
            return new IntSize {Width = (int) size.Width, Height = (int) size.Height};
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is IntSize size && Equals(size);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        public static bool operator ==(IntSize left, IntSize right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IntSize left, IntSize right)
        {
            return !left.Equals(right);
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
