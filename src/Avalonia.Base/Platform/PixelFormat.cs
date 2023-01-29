using System;

namespace Avalonia.Platform
{
    internal enum PixelFormatEnum
    {
        Rgb565,
        Rgba8888,
        Bgra8888,
        BlackWhite,
        Gray2,
        Gray4,
        Gray8,
        Gray16,
        Gray32Float,
        Rgba64
    }

    public struct PixelFormat : IEquatable<PixelFormat>
    {
        internal PixelFormatEnum FormatEnum;

        public int BitsPerPixel
        {
            get
            {
                if (FormatEnum == PixelFormatEnum.BlackWhite)
                    return 1;
                else if (FormatEnum == PixelFormatEnum.Gray2)
                    return 2;
                else if (FormatEnum == PixelFormatEnum.Gray4)
                    return 4;
                else if (FormatEnum == PixelFormatEnum.Gray8)
                    return 8;
                else if (FormatEnum == PixelFormatEnum.Rgb565 
                         || FormatEnum == PixelFormatEnum.Gray16)
                    return 16;
                else if (FormatEnum == PixelFormatEnum.Rgba64)
                    return 64;

                return 32;
            }
        }

        internal bool HasAlpha => FormatEnum == PixelFormatEnum.Rgba8888 
                                  || FormatEnum == PixelFormatEnum.Bgra8888
                                  || FormatEnum == PixelFormatEnum.Rgba64;

        internal PixelFormat(PixelFormatEnum format)
        {
            FormatEnum = format;
        }

        public static PixelFormat Rgb565 => PixelFormats.Rgb565;
        public static PixelFormat Rgba8888 => PixelFormats.Rgba8888;
        public static PixelFormat Bgra8888 => PixelFormats.Bgra8888;

        public bool Equals(PixelFormat other)
        {
            return FormatEnum == other.FormatEnum;
        }

        public override bool Equals(object? obj)
        {
            return obj is PixelFormat other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)FormatEnum;
        }

        public static bool operator ==(PixelFormat left, PixelFormat right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PixelFormat left, PixelFormat right)
        {
            return !left.Equals(right);
        }

        public override string ToString() => FormatEnum.ToString();
    }

    public static class PixelFormats
    {
        public static PixelFormat Rgb565 { get; } = new PixelFormat(PixelFormatEnum.Rgb565);
        public static PixelFormat Rgba8888 { get; } = new PixelFormat(PixelFormatEnum.Rgba8888);
        public static PixelFormat Rgba64 { get; } = new PixelFormat(PixelFormatEnum.Rgba64);
        public static PixelFormat Bgra8888 { get; } = new PixelFormat(PixelFormatEnum.Bgra8888);
        public static PixelFormat BlackWhite { get; } = new PixelFormat(PixelFormatEnum.BlackWhite);
        public static PixelFormat Gray2 { get; } = new PixelFormat(PixelFormatEnum.Gray2);
        public static PixelFormat Gray4 { get; } = new PixelFormat(PixelFormatEnum.Gray4);
        public static PixelFormat Gray8 { get; } = new PixelFormat(PixelFormatEnum.Gray8);
        public static PixelFormat Gray16 { get; } = new PixelFormat(PixelFormatEnum.Gray16);
        public static PixelFormat Gray32Float { get; } = new PixelFormat(PixelFormatEnum.Gray32Float);
    }
}
