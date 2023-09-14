using System;
namespace Avalonia.Media.Imaging;

internal interface IPixelFormatWriter
{
    void WriteNext(Rgba8888Pixel pixel);
    void Reset(IntPtr address);
}

internal static class PixelFormatWriter
{
    public unsafe struct Rgb24PixelFormatWriter : IPixelFormatWriter
    {
        private byte* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            addr[0] = pixel.R;
            addr[1] = pixel.G;
            addr[2] = pixel.B;

            _address += 3;
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Rgba64PixelFormatWriter : IPixelFormatWriter
    {
        private Rgba64Pixel* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            *addr = new Rgba64Pixel((ushort)(pixel.R << 8), (ushort)(pixel.G << 8), (ushort)(pixel.B << 8), (ushort)(pixel.A << 8));

            _address++;
        }

        public void Reset(IntPtr address) => _address = (Rgba64Pixel*)address;
    }

    public unsafe struct Rgba8888PixelFormatWriter : IPixelFormatWriter
    {
        private Rgba8888Pixel* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            *addr = pixel;

            _address++;
        }

        public void Reset(IntPtr address) => _address = (Rgba8888Pixel*)address;
    }

    public unsafe struct Bgra8888PixelFormatWriter : IPixelFormatWriter
    {
        private byte* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            addr[0] = pixel.B;
            addr[1] = pixel.G;
            addr[2] = pixel.R;
            addr[3] = pixel.A;

            _address += 4;
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Bgr24PixelFormatWriter : IPixelFormatWriter
    {
        private byte* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            addr[2] = pixel.R;
            addr[1] = pixel.G;
            addr[0] = pixel.B;

            _address += 3;
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Bgra32PixelFormatWriter : IPixelFormatWriter
    {
        private byte* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            addr[3] = pixel.A;
            addr[2] = pixel.R;
            addr[1] = pixel.G;
            addr[0] = pixel.B;

            _address += 4;
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Bgr565PixelFormatWriter : IPixelFormatWriter
    {
        private ushort* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            *addr = Pack(pixel);

            _address++;
        }

        public void Reset(IntPtr address) => _address = (ushort*)address;

        private static ushort Pack(Rgba8888Pixel pixel)
        {
            return (ushort)((((int)Math.Round(pixel.R / 255F * 31F) & 0x1F) << 11)
                  | (((int)Math.Round(pixel.G / 255F * 63F) & 0x3F) << 5)
                  | ((int)Math.Round(pixel.B / 255F * 31F) & 0x1F));
        }
    }

    public unsafe struct Bgr555PixelFormatWriter : IPixelFormatWriter
    {
        private ushort* _address;
        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            *addr = Pack(pixel);

            _address++;
        }

        public void Reset(IntPtr address) => _address = (ushort*)address;

        private static ushort Pack(Rgba8888Pixel pixel)
        {
            return (ushort)(
              (((int)Math.Round(pixel.R / 255F * 31F) & 0x1F) << 10)
              | (((int)Math.Round(pixel.G / 255F * 31F) & 0x1F) << 5)
              | (((int)Math.Round(pixel.B / 255F * 31F) & 0x1F) << 0));
        }
    }

    public unsafe struct Gray32FloatPixelFormatWriter : IPixelFormatWriter
    {
        private float* _address;

        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            *addr = Pack(pixel);

            _address++;
        }

        private static float Pack(Rgba8888Pixel pixel)
        {
            return (float)Math.Pow(pixel.R / 255F, 2.2);
        }

        public void Reset(IntPtr address) => _address = (float*)address;
    }

    public unsafe struct BlackWhitePixelFormatWriter : IPixelFormatWriter
    {
        private int _bit;
        private byte* _address;

        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            var grayscale = Math.Round(0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B);

            var value = grayscale > 0x7F ? 1 : 0;

            var shift = 7 - _bit;
            var mask = 1 << shift;

            *addr = (byte)((*addr & ~mask) | value << shift);

            _bit++;

            if (_bit == 8)
            {
                _address++;

                _bit = 0;
            }
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Gray2PixelFormatWriter : IPixelFormatWriter
    {
        private int _bit;
        private byte* _address;

        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;
            var value = 0;

            var grayscale = (byte)Math.Round(0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B);

            if (grayscale > 0 && grayscale <= 0x55)
            {
                //01
                value = 1;
            }

            if (grayscale > 0x55 && grayscale <= 0xAA)
            {
                //10

                value = 2;
            }

            if (grayscale > 0xAA)
            {
                //11
                value = 3;
            }

            var shift = 6 - _bit;
            var mask = 3 << shift;

            *addr = (byte)((*addr & ~mask) | value << shift);

            _bit += 2;

            if (_bit == 8)
            {
                _address++;
                _bit = 0;
            }
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Gray4PixelFormatWriter : IPixelFormatWriter
    {
        private int _bit;
        private byte* _address;

        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            var grayscale = (byte)Math.Round(0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B);

            var value = (byte)(grayscale / 255F * 0xF);

            var shift = 4 - _bit;
            var mask = 0xF << shift;

            *addr = (byte)((*addr & ~mask) | value << shift);

            _bit += 4;

            if (_bit == 8)
            {
                _address++;
                _bit = 0;
            }
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Gray8PixelFormatWriter : IPixelFormatWriter
    {
        private byte* _address;

        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            var grayscale = (byte)Math.Round(0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B);

            *addr = grayscale;

            _address++;
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Gray16PixelFormatWriter : IPixelFormatWriter
    {
        private ushort* _address;

        public void WriteNext(Rgba8888Pixel pixel)
        {
            var addr = _address;

            var grayscale = (ushort)Math.Round((0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B) * 0x0101);

            *addr = grayscale;

            _address++;
        }

        public void Reset(IntPtr address) => _address = (ushort*)address;
    }
}


