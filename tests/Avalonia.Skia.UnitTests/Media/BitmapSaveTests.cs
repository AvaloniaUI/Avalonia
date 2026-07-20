using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class BitmapSaveTests
    {
        [Fact]
        public void Save_With_Null_Options_Throws()
        {
            using var app = Start();
            
            using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
            using var stream = new MemoryStream();

            Assert.Throws<ArgumentNullException>(() => bitmap.Save(stream, (BitmapEncoderOptions)null!));
        }
        
        [Fact]
        public void Save_With_Invalid_Png_CompressionLevel_Throws()
        {
            using var app = Start();
            
            using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
            using var stream = new MemoryStream();
            var options = new PngBitmapEncoderOptions { CompressionLevel = (CompressionLevel)42 };

            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Save(stream, options));
        }
        
        [Fact]
        public void Save_With_Invalid_Jpeg_Quality_Throws()
        {
            using var app = Start();
            
            using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
            using var stream = new MemoryStream();
            var options = new JpegBitmapEncoderOptions { Quality = -1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Save(stream, options));
        }

        [Fact]
        public void Save_With_Png_Options_Produces_Png()
        {
            using var app = Start();
            
            using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
            using var stream = new MemoryStream();

            bitmap.Save(stream, PngBitmapEncoderOptions.Default);

            stream.Position = 0;
            using var codec = SKCodec.Create(stream);

            Assert.Equal(SKEncodedImageFormat.Png, codec.EncodedFormat);
        }

        [Fact]
        public void Save_With_Jpeg_Options_Produces_Jpeg()
        {
            using var app = Start();
            
            using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
            using var stream = new MemoryStream();

            bitmap.Save(stream, JpegBitmapEncoderOptions.Default);
                
            stream.Position = 0;
            using var codec = SKCodec.Create(stream);

            Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        }

        private static WriteableBitmap CreateBitmap(SKColor color, int width, int height)
        {
            var pixel = (color.Alpha << 24) | (color.Red << 16) | (color.Green << 8) | color.Blue;

            var data = new int[width * height];
            data.AsSpan().Fill(pixel);

            return CreateBitmap(width, height, data);
        }

        private static WriteableBitmap CreateBitmap(int width, int height, int[] data)
        {
            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

            using var fb = bitmap.Lock();
            
            for (var y = 0; y < height; y++)
                Marshal.Copy(data, y * width, fb.Address + y * fb.RowBytes, width);

            return bitmap;
        }

        private static IDisposable Start()
            => UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface()));
    }
}
