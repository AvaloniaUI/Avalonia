using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class ImmutableBitmapTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(3, 5)]
        [InlineData(64, 64)]
        public unsafe void Constructor_From_Pixels_Copies_Source_Data(int width, int height)
        {
            var size = new PixelSize(width, height);
            var stride = width * 4;
            var byteSize = stride * height;

            var source = Marshal.AllocHGlobal(byteSize);
            try
            {
                var src = new Span<byte>((void*)source, byteSize);
                for (var i = 0; i < src.Length; i++)
                    src[i] = (byte)(i * 7 + 1);

                using var bitmap = new ImmutableBitmap(
                    size, new Vector(96, 96), stride,
                    PixelFormat.Bgra8888, AlphaFormat.Premul, source);

                // The constructor must take its own copy: corrupting (and freeing) the source
                // afterwards must not affect the bitmap's pixels.
                src.Fill(0xCD);

                Assert.Equal(size, bitmap.PixelSize);

                using var locked = bitmap.Lock();
                Assert.Equal(size, locked.Size);

                var dst = new ReadOnlySpan<byte>((void*)locked.Address, byteSize);
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width * 4; x++)
                    {
                        var srcIndex = y * stride + x;
                        var dstIndex = y * locked.RowBytes + x;
                        Assert.Equal((byte)(srcIndex * 7 + 1), dst[dstIndex]);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(source);
            }
        }
    }
}
