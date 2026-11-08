using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class BitmapCopyPixelsTests
    {
        [Fact]
        public unsafe void CopyPixels_To_Framebuffer_Uses_Destination_RowBytes()
        {
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface()));

            const int width = 3;
            const int height = 2;
            using var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Premul);

            using (var fb = bitmap.Lock())
            {
                for (var y = 0; y < height; y++)
                {
                    var row = (uint*)(fb.Address + fb.RowBytes * y);
                    for (var x = 0; x < width; x++)
                        row[x] = GetPixel(x, y);
                }
            }

            const int destRowBytes = width * 4 + 8;
            var dest = new byte[destRowBytes * height];

            fixed (byte* pDest = dest)
            {
                using (var destFb = new LockedFramebuffer((IntPtr)pDest, new PixelSize(width, height), destRowBytes,
                           new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul, null))
                {
                    bitmap.CopyPixels(destFb);
                }

                for (var y = 0; y < height; y++)
                {
                    var row = (uint*)(pDest + destRowBytes * y);
                    for (var x = 0; x < width; x++)
                        Assert.Equal(GetPixel(x, y), row[x]);
                }
            }
        }

        [Theory]
        [InlineData(false, 2, 2, 12)]
        [InlineData(false, 3, 1, 12)]
        [InlineData(false, 3, 2, 8)]
        [InlineData(true, 2, 2, 12)]
        [InlineData(true, 3, 1, 12)]
        [InlineData(true, 3, 2, 8)]
        public unsafe void CopyPixels_To_Undersized_Framebuffer_Throws(bool convert, int width, int height, int rowBytes)
        {
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface()));

            using var bitmap = new WriteableBitmap(new PixelSize(3, 2), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Premul);

            const byte guard = 0xA5;
            var dest = new byte[256];
            dest.AsSpan().Fill(guard);

            fixed (byte* pDest = dest)
            {
                using var destFb = new LockedFramebuffer((IntPtr)pDest, new PixelSize(width, height), rowBytes,
                    new Vector(96, 96), convert ? PixelFormat.Rgba8888 : PixelFormat.Bgra8888, AlphaFormat.Premul,
                    null);

                Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.CopyPixels(destFb));
            }

            Assert.All(dest, b => Assert.Equal(guard, b));
        }

        private static uint GetPixel(int x, int y) => 0xFF000000u | (uint)(y * 0x40 + x * 0x10 + 1);
    }
}
