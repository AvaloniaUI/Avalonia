using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia.RenderTests;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class BitmapLoadingTests : TestBase
{
    public BitmapLoadingTests()
        :base("BitmapLoadingTests")
    {
    }
    
    [Theory]
    [InlineData("1.jpg")]
    public void DecodeToWidth_ShouldNotThrowAccessViolation(string imageName)
    {
        try
        {
                    using var fs = File.Open(GetImageFileName(imageName), FileMode.Open, FileAccess.Read,
                        FileShare.Read);
                    using var bmp = Bitmap.DecodeToWidth(fs, 50);
        }
        catch (AccessViolationException exception)
        {
            // Fail
            Assert.True(false);
        }
    }


    private static string GetImageFileName(string imageName)
    {
        var path = Directory.GetCurrentDirectory();

        while (path.Length > 0 && Path.GetFileName(path) != "Avalonia.Skia.UnitTests")
        {
            path = Path.GetDirectoryName(path);
        }

        return Path.Combine(path, "Images", imageName);
    }
}
