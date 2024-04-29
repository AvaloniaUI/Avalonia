using System;
using Avalonia.Media.Imaging;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Imaging
{
    public class PixelFormatWriterTests
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

        [Fact]
        public void Should_Write_Bgr555()
        {
            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Bgr555), 
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Bgr555PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Bgr555PixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            Assert.Equal(new Rgba8888Pixel { R = 255, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { G = 255 });
            Assert.Equal(new Rgba8888Pixel { G = 255, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { B = 255 });
            Assert.Equal(new Rgba8888Pixel { B = 255, A = 255 }, pixelReader.ReadNext());
        }

        [Fact]
        public void Should_Write_Bgra8888()
        {
            var sourceMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Bgra8888), 
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(3, 1));

            var sourceWriter = new PixelFormatWriter.Bgra8888PixelFormatWriter();
            var sourceReader = new PixelFormatReader.Bgra8888PixelFormatReader();

            sourceWriter.Reset(sourceMemory.Address);
            sourceReader.Reset(sourceMemory.Address);

            sourceWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            Assert.Equal(new Rgba8888Pixel { R = 255 }, sourceReader.ReadNext());

            sourceWriter.WriteNext(new Rgba8888Pixel { G = 255 });
            Assert.Equal(new Rgba8888Pixel { G = 255 }, sourceReader.ReadNext());

            sourceWriter.WriteNext(new Rgba8888Pixel { B = 255 });
            Assert.Equal(new Rgba8888Pixel { B = 255 }, sourceReader.ReadNext());
        }

        [Fact]
        public void Should_Write_Rgba8888()
        {
            var sourceMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Rgba8888),
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Rgba8888PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Rgba8888PixelFormatReader();

            pixelWriter.Reset(sourceMemory.Address);
            pixelReader.Reset(sourceMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 }, pixelReader.ReadNext());
        }

        [Fact]
        public void Should_Write_Rgb24()
        {
            var sourceMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Rgb24),
                Platform.AlphaFormat.Unpremul,
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Rgb24PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Rgb24PixelFormatReader();

            pixelWriter.Reset(sourceMemory.Address);
            pixelReader.Reset(sourceMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255, G = 125, B = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 255, B = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 125, B = 255 });
            Assert.Equal(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 255 }, pixelReader.ReadNext());
        }


        [Fact]
        public void Should_Write_Rgba64()
        {
            var sourceMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Rgba64),
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Rgba64PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Rgba64PixelFormatReader();

            pixelWriter.Reset(sourceMemory.Address);
            pixelReader.Reset(sourceMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 }, pixelReader.ReadNext());
        }

        [Fact]
        public void Should_Write_Bgr565()
        {
            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Bgr565),
                Platform.AlphaFormat.Unpremul,
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Bgr565PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Bgr565PixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            Assert.Equal(new Rgba8888Pixel { R = 255, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { G = 255 });
            Assert.Equal(new Rgba8888Pixel { G = 255, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { B = 255 });
            Assert.Equal(new Rgba8888Pixel { B = 255, A = 255 }, pixelReader.ReadNext());
        }

        [Fact]
        public void Should_Write_Gray32Float()
        {
            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Gray32Float), 
                Platform.AlphaFormat.Unpremul,
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Gray32FloatPixelFormatWriter();
            var pixelReader = new PixelFormatReader.Gray32FloatPixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            Assert.Equal(new Rgba8888Pixel { R = 255, G = 255, B = 255, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 125 });
            Assert.Equal(new Rgba8888Pixel { R = 125, G = 125, B = 125, A = 255 }, pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel());
            Assert.Equal(new Rgba8888Pixel { A = 255 }, pixelReader.ReadNext());
        }

        [Fact]
        public void Should_Write_BlackWhite()
        {
            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.BlackWhite),
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.BlackWhitePixelFormatWriter();
            var pixelReader = new PixelFormatReader.BlackWhitePixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(s_white);
            Assert.Equal(s_white, pixelReader.ReadNext());

            pixelWriter.WriteNext(s_black);
            Assert.Equal(s_black, pixelReader.ReadNext());
        }

        [Fact]
        public void Should_Write_Gray2()
        {
            var palette = new[]
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

            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Gray2),
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Gray2PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Gray2PixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(palette[0]);
            Assert.Equal(palette[0], pixelReader.ReadNext());

            pixelWriter.WriteNext(palette[1]);
            Assert.Equal(palette[1], pixelReader.ReadNext());

            pixelWriter.WriteNext(palette[2]);
            Assert.Equal(palette[2], pixelReader.ReadNext());

            pixelWriter.WriteNext(palette[3]);
            Assert.Equal(palette[3], pixelReader.ReadNext());
        }

        [Fact]
        public void Should_Write_Gray4()
        {
            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Gray4),
                Platform.AlphaFormat.Unpremul,
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Gray4PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Gray4PixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            Assert.Equal(GetGray4(new Rgba8888Pixel { R = 255 }), pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 17 });
            Assert.Equal(GetGray4(new Rgba8888Pixel { R = 17 }), pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel());
            Assert.Equal(new Rgba8888Pixel { A = 255 }, pixelReader.ReadNext());
        }

        private static Rgba8888Pixel GetGray4(Rgba8888Pixel pixel)
        {
            var grayscale = (byte)Math.Round(0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B);

            var value = (byte)(grayscale / 255F * 0xF);

            value = (byte)(value | (value << 4));

            return new Rgba8888Pixel(value, value, value, 255);
        }

        [Fact]
        public void Should_Write_Gray8()
        {
            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Gray8),
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Gray8PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Gray8PixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            Assert.Equal(GetGray8(new Rgba8888Pixel { R = 255 }), pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 120 });
            Assert.Equal(GetGray8(new Rgba8888Pixel { R = 120 }), pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel());
            Assert.Equal(GetGray8(new Rgba8888Pixel { A = 255 }), pixelReader.ReadNext());
        }

        private static Rgba8888Pixel GetGray8(Rgba8888Pixel pixel)
        {
            var value = (byte)Math.Round(0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B);

            return new Rgba8888Pixel(value, value, value, 255);
        }

        [Fact]
        public void Should_Write_Gray16()
        {
            var bitmapMemory = new BitmapMemory(
                new Platform.PixelFormat(Platform.PixelFormatEnum.Gray16),
                Platform.AlphaFormat.Unpremul, 
                new PixelSize(10, 10));

            var pixelWriter = new PixelFormatWriter.Gray16PixelFormatWriter();
            var pixelReader = new PixelFormatReader.Gray16PixelFormatReader();

            pixelWriter.Reset(bitmapMemory.Address);
            pixelReader.Reset(bitmapMemory.Address);

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
            Assert.Equal(GetGray16(new Rgba8888Pixel { R = 255 }), pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel { R = 120 });
            Assert.Equal(GetGray16(new Rgba8888Pixel { R = 120 }), pixelReader.ReadNext());

            pixelWriter.WriteNext(new Rgba8888Pixel());
            Assert.Equal(GetGray16(new Rgba8888Pixel { A = 255 }), pixelReader.ReadNext());
        }

        private static Rgba8888Pixel GetGray16(Rgba8888Pixel pixel)
        {
            var grayscale = (ushort)Math.Round((0.299F * pixel.R + 0.587F * pixel.G + 0.114F * pixel.B) * 0x0101);

            var value = (byte)(grayscale >> 8);

            return new Rgba8888Pixel(value, value, value, 255);
        }
    }
}
