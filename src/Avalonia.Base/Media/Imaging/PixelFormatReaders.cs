using System;
using Avalonia.Platform;
namespace Avalonia.Media.Imaging;

internal record struct Rgba64Pixel
{
    public Rgba64Pixel(ushort r, ushort g, ushort b, ushort a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public ushort R;
    public ushort G;
    public ushort B;
    public ushort A;
}

internal record struct Rgba8888Pixel
{
    public Rgba8888Pixel(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public byte R;
    public byte G;
    public byte B;
    public byte A;
}

internal interface IPixelFormatReader
{
    Rgba8888Pixel ReadNext();
    void Reset(IntPtr address);
}

internal static unsafe class PixelFormatReader
{
    private static readonly Rgba8888Pixel s_white = new Rgba8888Pixel
    {
        A = 255,
        B = 255,
        G = 255,
        R = 255
    };

    private static readonly Rgba8888Pixel s_black = new Rgba8888Pixel
    {
        A = 255,
        B = 0,
        G = 0,
        R = 0
    };

    public unsafe struct BlackWhitePixelFormatReader : IPixelFormatReader
    {
        private int _bit;
        private byte* _address;

        public void Reset(IntPtr address)
        {
            _address = (byte*)address;
            _bit = 0;
        }

        public Rgba8888Pixel ReadNext()
        {
            var shift = 7 - _bit;
            var value = (*_address >> shift) & 1;
            _bit++;
            if (_bit == 8)
            {
                _address++;
                _bit = 0;
            }
            return value == 1 ? s_white : s_black;
        }
    }

    public unsafe struct Gray2PixelFormatReader : IPixelFormatReader
    {
        private int _bit;
        private byte* _address;

        public void Reset(IntPtr address)
        {
            _address = (byte*)address;
            _bit = 0;
        }

        private static readonly Rgba8888Pixel[] Palette = new[]
        {
            s_black,
            new Rgba8888Pixel
            {
                A = 255, B = 0x55, G = 0x55, R = 0x55
            },
            new Rgba8888Pixel
            {
                A = 255, B = 0xAA, G = 0xAA, R = 0xAA
            },
            s_white
        };

        public Rgba8888Pixel ReadNext()
        {
            var shift = 6 - _bit;
            var value = (byte)((*_address >> shift));
            value = (byte)((value & 3));
            _bit += 2;
            if (_bit == 8)
            {
                _address++;
                _bit = 0;
            }

            return Palette[value];
        }
    }

    public unsafe struct Gray4PixelFormatReader : IPixelFormatReader
    {
        private int _bit;
        private byte* _address;

        public void Reset(IntPtr address)
        {
            _address = (byte*)address;
            _bit = 0;
        }

        public Rgba8888Pixel ReadNext()
        {
            var shift = 4 - _bit;
            var value = (byte)((*_address >> shift));
            value = (byte)((value & 0xF));
            value = (byte)(value | (value << 4));
            _bit += 4;
            if (_bit == 8)
            {
                _address++;
                _bit = 0;
            }

            return new Rgba8888Pixel
            {
                A = 255,
                B = value,
                G = value,
                R = value
            };
        }
    }

    public unsafe struct Gray8PixelFormatReader : IPixelFormatReader
    {
        private byte* _address;
        public void Reset(IntPtr address)
        {
            _address = (byte*)address;
        }

        public Rgba8888Pixel ReadNext()
        {
            var value = *_address;
            _address++;

            return new Rgba8888Pixel
            {
                A = 255,
                B = value,
                G = value,
                R = value
            };
        }
    }

    public unsafe struct Gray16PixelFormatReader : IPixelFormatReader
    {
        private ushort* _address;
        public Rgba8888Pixel ReadNext()
        {
            var value16 = *_address;
            _address++;
            var value8 = (byte)(value16 >> 8);
            return new Rgba8888Pixel
            {
                A = 255,
                B = value8,
                G = value8,
                R = value8
            };
        }

        public void Reset(IntPtr address) => _address = (ushort*)address;
    }

    public unsafe struct Gray32FloatPixelFormatReader : IPixelFormatReader
    {
        private byte* _address;
        public Rgba8888Pixel ReadNext()
        {
            var f = *(float*)_address;
            var srgb = Math.Pow(f, 1 / 2.2);
            var value = (byte)(srgb * 255);

            _address += 4;
            return new Rgba8888Pixel
            {
                A = 255,
                B = value,
                G = value,
                R = value
            };
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }


    public unsafe struct Rgba64PixelFormatReader : IPixelFormatReader
    {
        private Rgba64Pixel* _address;
        public Rgba8888Pixel ReadNext()
        {
            var value = *_address;

            _address++;
            return new Rgba8888Pixel
            {
                A = (byte)(value.A >> 8),
                B = (byte)(value.B >> 8),
                G = (byte)(value.G >> 8),
                R = (byte)(value.R >> 8),
            };
        }

        public void Reset(IntPtr address) => _address = (Rgba64Pixel*)address;
    }

    public unsafe struct Rgb24PixelFormatReader : IPixelFormatReader
    {
        private byte* _address;
        public Rgba8888Pixel ReadNext()
        {
            var addr = _address;
            _address += 3;
            return new Rgba8888Pixel
            {
                R = addr[0],
                G = addr[1],
                B = addr[2],
                A = 255,
            };
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Bgr24PixelFormatReader : IPixelFormatReader
    {
        private byte* _address;
        public Rgba8888Pixel ReadNext()
        {
            var addr = _address;
            _address += 3;
            return new Rgba8888Pixel
            {
                R = addr[2],
                G = addr[1],
                B = addr[0],
                A = 255,
            };
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public unsafe struct Bgr555PixelFormatReader : IPixelFormatReader
    {
        private byte* _address;
        public Rgba8888Pixel ReadNext()
        {
            var addr = (ushort*)_address;

            _address += 2;

            return UnPack(*addr);
        }

        public void Reset(IntPtr address) => _address = (byte*)address;

        private static Rgba8888Pixel UnPack(ushort value)
        {
            var r = (byte)Math.Round(((value >> 10) & 0x1F) / 31F * 255);
            var g = (byte)Math.Round(((value >> 5) & 0x1F) / 31F * 255);
            var b = (byte)Math.Round(((value >> 0) & 0x1F) / 31F * 255);

            return new Rgba8888Pixel(r, g, b, 255);
        }
    }

    public unsafe struct Bgr565PixelFormatReader : IPixelFormatReader
    {
        private byte* _address;
        public Rgba8888Pixel ReadNext()
        {
            var addr = (ushort*)_address;

            _address += 2;

            return UnPack(*addr);
        }

        public void Reset(IntPtr address) => _address = (byte*)address;

        private static Rgba8888Pixel UnPack(ushort value)
        {
            var r = (byte)Math.Round(((value >> 11) & 0x1F) / 31F * 255);
            var g = (byte)Math.Round(((value >> 5) & 0x3F) / 63F * 255);
            var b = (byte)Math.Round(((value >> 0) & 0x1F) / 31F * 255);

            return new Rgba8888Pixel(r, g, b, 255);
        }
    }

    public unsafe struct Rgba8888PixelFormatReader : IPixelFormatReader
    {
        private Rgba8888Pixel* _address;
        public Rgba8888Pixel ReadNext()
        {
            var value = *_address;

            _address++;

            return value;
        }

        public void Reset(IntPtr address) => _address = (Rgba8888Pixel*)address;
    }

    public unsafe struct Bgra8888PixelFormatReader : IPixelFormatReader
    {
        private byte* _address;
        public Rgba8888Pixel ReadNext()
        {
            var addr = _address;

            _address += 4;

            return new Rgba8888Pixel(addr[2], addr[1], addr[0], addr[3]);
        }

        public void Reset(IntPtr address) => _address = (byte*)address;
    }

    public static bool SupportsFormat(PixelFormat format)
    {
        switch (format.FormatEnum)
        {
            case PixelFormatEnum.Rgb565:
            case PixelFormatEnum.Rgba8888:
            case PixelFormatEnum.Bgra8888:
            case PixelFormatEnum.BlackWhite:
            case PixelFormatEnum.Gray2:
            case PixelFormatEnum.Gray4:
            case PixelFormatEnum.Gray8:
            case PixelFormatEnum.Gray16:
            case PixelFormatEnum.Gray32Float:
            case PixelFormatEnum.Rgba64:
            case PixelFormatEnum.Rgb24:
            case PixelFormatEnum.Bgr24:
            case PixelFormatEnum.Bgr555:
            case PixelFormatEnum.Bgr565:
                return true;
            default:
                return false;
        }
    } 
}
