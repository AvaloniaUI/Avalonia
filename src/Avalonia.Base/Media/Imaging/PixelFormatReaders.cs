using System;
using Avalonia.Platform;
namespace Avalonia.Media.Imaging;

internal struct Rgba8888Pixel
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;
}

static unsafe class PixelFormatReader
{
    public interface IPixelFormatReader
    {
        Rgba8888Pixel ReadNext();
        void Reset(IntPtr address);
    }
    
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

    public unsafe struct BlackWhitePixelReader : IPixelFormatReader
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
    
    public unsafe struct Gray2PixelReader : IPixelFormatReader
    {
        private int _bit;
        private byte* _address;

        public void Reset(IntPtr address)
        {
            _address = (byte*)address;
            _bit = 0;
        }

        private static Rgba8888Pixel[] Palette = new[]
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
    
    public unsafe struct Gray4PixelReader : IPixelFormatReader
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
    
    public unsafe struct Gray8PixelReader : IPixelFormatReader
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
    
    public unsafe struct Gray16PixelReader : IPixelFormatReader
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

    public unsafe struct Gray32FloatPixelReader : IPixelFormatReader
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

    struct Rgba64
    {
#pragma warning disable CS0649
        public ushort R;
        public ushort G;
        public ushort B;
        public ushort A;
#pragma warning restore CS0649
    }

    public unsafe struct Rgba64PixelFormatReader : IPixelFormatReader
    {
        private Rgba64* _address;
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

        public void Reset(IntPtr address) => _address = (Rgba64*)address;
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

    public static void Transcode(IntPtr dst, IntPtr src, PixelSize size, int strideSrc, int strideDst,
        PixelFormat format)
    {
        if (format == PixelFormats.BlackWhite)
            Transcode<BlackWhitePixelReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Gray2)
            Transcode<Gray2PixelReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Gray4)
            Transcode<Gray4PixelReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Gray8)
            Transcode<Gray8PixelReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Gray16)
            Transcode<Gray16PixelReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Rgb24)
            Transcode<Rgb24PixelFormatReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Bgr24)
            Transcode<Bgr24PixelFormatReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Gray32Float)
            Transcode<Gray32FloatPixelReader>(dst, src, size, strideSrc, strideDst);
        else if (format == PixelFormats.Rgba64)
            Transcode<Rgba64PixelFormatReader>(dst, src, size, strideSrc, strideDst);
        else
            throw new NotSupportedException($"Pixel format {format} is not supported");
    }
    
    public static bool SupportsFormat(PixelFormat format)
    {
        return format == PixelFormats.BlackWhite
               || format == PixelFormats.Gray2
               || format == PixelFormats.Gray4
               || format == PixelFormats.Gray8
               || format == PixelFormats.Gray16
               || format == PixelFormats.Gray32Float
               || format == PixelFormats.Rgba64
               || format == PixelFormats.Bgr24
               || format == PixelFormats.Rgb24;
    }
    
    public static void Transcode<TReader>(IntPtr dst, IntPtr src, PixelSize size, int strideSrc, int strideDst) where TReader : struct, IPixelFormatReader
    {
        var w = size.Width;
        var h = size.Height;
        TReader reader = default;
        for (var y = 0; y < h; y++)
        {
            reader.Reset(src + strideSrc * y);
            var dstRow = (Rgba8888Pixel*)(dst + strideDst * y);
            for (var x = 0; x < w; x++)
            {
                *dstRow = reader.ReadNext();
                dstRow++;
            }
        }
    }
}