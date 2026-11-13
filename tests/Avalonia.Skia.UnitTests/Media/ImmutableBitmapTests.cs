using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class ImmutableBitmapTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Constructor_From_Invalid_Encoded_Data_Throws_ArgumentException(bool horizontal)
        {
            using var stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });

            var exception = Assert.Throws<ArgumentException>(() =>
                new ImmutableBitmap(stream, 48, horizontal, BitmapInterpolationMode.HighQuality));

            Assert.Equal("Unable to load bitmap from provided data", exception.Message);
        }

        [Theory]
        [InlineData(1, 1, false)]
        [InlineData(3, 5, false)]
        [InlineData(64, 64, false)]
        [InlineData(1, 1, true)]
        [InlineData(3, 5, true)]
        [InlineData(64, 64, true)]
        public unsafe void Constructor_From_Pixels_Copies_Source_Data(int width, int height, bool negativeStride)
        {
            var size = new PixelSize(width, height);
            var rowBytes = width * 4;
            var absStride = rowBytes;
            var byteSize = absStride * height;

            // Logical pixel byte: deterministic function of (row, byteIndexWithinRow).
            byte Expected(int row, int x) => (byte)((row * rowBytes + x) * 7 + 1);

            var source = Marshal.AllocHGlobal(byteSize);
            try
            {
                // Lay the logical rows out in physical memory. For a negative stride the rows are stored
                // bottom-up and the data pointer addresses the first (top) logical row, which sits at the
                // highest address.
                var buffer = new Span<byte>((void*)source, byteSize);
                for (var row = 0; row < height; row++)
                {
                    var physicalRow = negativeStride ? height - 1 - row : row;
                    for (var x = 0; x < rowBytes; x++)
                        buffer[physicalRow * absStride + x] = Expected(row, x);
                }

                var stride = negativeStride ? -absStride : absStride;
                var data = negativeStride ? source + absStride * (height - 1) : source;

                using var bitmap = new ImmutableBitmap(
                    size, new Vector(96, 96), stride,
                    PixelFormat.Bgra8888, AlphaFormat.Premul, data);

                // The constructor must take its own copy: corrupting (and freeing) the source
                // afterwards must not affect the bitmap's pixels.
                buffer.Fill(0xCD);

                Assert.Equal(size, bitmap.PixelSize);

                using var locked = bitmap.Lock();
                Assert.Equal(size, locked.Size);

                var dst = new ReadOnlySpan<byte>((void*)locked.Address, locked.RowBytes * height);
                for (var row = 0; row < height; row++)
                    for (var x = 0; x < rowBytes; x++)
                        Assert.Equal(Expected(row, x), dst[row * locked.RowBytes + x]);
            }
            finally
            {
                Marshal.FreeHGlobal(source);
            }
        }
    }
}
