using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class BitmapMemoryTests
{
    [InlineData(PixelFormatEnum.Bgr24, AlphaFormat.Opaque)]
    [InlineData(PixelFormatEnum.Bgr555, AlphaFormat.Opaque)]
    [InlineData(PixelFormatEnum.Bgr565, AlphaFormat.Opaque)]
    [InlineData(PixelFormatEnum.BlackWhite, AlphaFormat.Opaque)]
    [Theory]
    internal void Should_Align_RowBytes_To_Four_Bytes(PixelFormatEnum pixelFormatEnum, AlphaFormat alphaFormat)
    {
        var bitmapMemory = new BitmapMemory(new PixelFormat(pixelFormatEnum), alphaFormat, new PixelSize(33, 1));
        
        Assert.True(bitmapMemory.RowBytes % 4 == 0);
    }
}
