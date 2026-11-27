using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Imaging
{
    public class PixelFormatTranscoderTests
    {
        [Fact]
        public void Should_Transcode()
        {
            var sourceMemory = CreateBitmapMemory();

            var destMemory = new BitmapMemory(PixelFormat.Bgra8888, AlphaFormat.Opaque, sourceMemory.Size);

            PixelFormatTranscoder.Transcode(
                sourceMemory.Address,
                sourceMemory.Size,
                sourceMemory.RowBytes,
                sourceMemory.Format,
                sourceMemory.AlphaFormat,
                destMemory.Address,
                destMemory.RowBytes,
                destMemory.Format,
                destMemory.AlphaFormat);

            var reader = new PixelFormatReader.Bgra8888PixelFormatReader();

            reader.Reset(destMemory.Address);

            Assert.Equal(new Rgba8888Pixel(255, 0, 0, 0), reader.ReadNext());
            Assert.Equal(new Rgba8888Pixel(0, 255, 0, 0), reader.ReadNext());
            Assert.Equal(new Rgba8888Pixel(0, 0, 255, 0), reader.ReadNext());
        }

        [Theory]
        [InlineData((int)PixelFormatEnum.BlackWhite)]
        [InlineData((int)PixelFormatEnum.Gray2)]
        [InlineData((int)PixelFormatEnum.Gray4)]
        public unsafe void Should_Start_Each_Row_At_First_Bit_For_Packed_Formats(int formatValue)
        {
            var formatEnum = (PixelFormatEnum)formatValue;
            const int width = 3;
            const int height = 2;
            const byte guard = 0xA5;

            var black = new Rgba8888Pixel(0, 0, 0, 255);
            var white = new Rgba8888Pixel(255, 255, 255, 255);
            var source = new[] { white, black, white, black, white, black };

            var format = new PixelFormat(formatEnum);
            var stride = (width * format.BitsPerPixel + 7) / 8;
            var dest = new byte[stride * height + 4];
            dest.AsSpan().Fill(guard);

            fixed (Rgba8888Pixel* pSource = source)
            fixed (byte* pDest = dest)
            {
                PixelFormatTranscoder.Transcode(
                    (IntPtr)pSource,
                    new PixelSize(width, height),
                    width * 4,
                    PixelFormat.Rgba8888,
                    AlphaFormat.Opaque,
                    (IntPtr)pDest,
                    stride,
                    format,
                    AlphaFormat.Opaque);

                for (var y = 0; y < height; y++)
                {
                    var reader = CreatePackedReader(formatEnum);
                    reader.Reset((IntPtr)(pDest + stride * y));

                    for (var x = 0; x < width; x++)
                        Assert.Equal(source[y * width + x], reader.ReadNext());
                }
            }

            for (var i = stride * height; i < dest.Length; i++)
                Assert.Equal(guard, dest[i]);
        }

        private static IPixelFormatReader CreatePackedReader(PixelFormatEnum format) => format switch
        {
            PixelFormatEnum.BlackWhite => new PixelFormatReader.BlackWhitePixelFormatReader(),
            PixelFormatEnum.Gray2 => new PixelFormatReader.Gray2PixelFormatReader(),
            PixelFormatEnum.Gray4 => new PixelFormatReader.Gray4PixelFormatReader(),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        private BitmapMemory CreateBitmapMemory()
        {
            var bitmapMemory = new BitmapMemory(PixelFormat.Rgba8888, AlphaFormat.Opaque, new PixelSize(3, 1));

            var sourceWriter = new PixelFormatWriter.Rgba8888PixelFormatWriter();

            sourceWriter.Reset(bitmapMemory.Address);

            sourceWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            sourceWriter.WriteNext(new Rgba8888Pixel { G = 255 });
            sourceWriter.WriteNext(new Rgba8888Pixel { B = 255 });

            return bitmapMemory;
        }
    }
}
