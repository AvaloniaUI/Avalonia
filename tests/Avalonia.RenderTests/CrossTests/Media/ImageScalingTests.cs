#nullable enable

using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using CrossUI;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests.CrossTests;
#else
namespace Avalonia.RenderTests.WpfCompare.CrossTests;
#endif

public class ImageScalingTests() : CrossTestBase("Media/ImageScaling")
{
    [CrossFact]
    public void Upscaling_With_HighQuality_Should_Be_Antialiased()
        => TestHighQualityScaling(1024);

    [CrossFact]
    public void Downscaling_With_HighQuality_Should_Be_Antialiased()
        => TestHighQualityScaling(128);

    private void TestHighQualityScaling(int size, [CallerMemberName] string? testName = null)
    {
        var directoryPath = Path.GetDirectoryName(typeof(ImageScalingTests).Assembly.Location);
        var imagePath = Path.Join(directoryPath, "Assets", "Star512.png");

        RenderAndCompare(
            new CrossImageControl
            {
                Width = size,
                Height = size,
                Image = new CrossBitmapImage(imagePath),
                BitmapInterpolationMode = BitmapInterpolationMode.HighQuality
            },
            testName);
    }
}
