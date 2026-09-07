#if AVALONIA_SKIA
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.UnitTests;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Path = System.IO.Path;

namespace Avalonia.Skia.RenderTests;

public class RenderTargetBitmapTests : TestBase
{
    public RenderTargetBitmapTests() : base(@"Media\RenderTargetBitmap")
    {
    }

    [Theory]
    [InlineData(false, 3, 96, false, false)]
    [InlineData(false, -3, 96, false, false)]
    [InlineData(false, 3, 144, false, false)]
    [InlineData(true, 3, 96, false, false)]
    [InlineData(true, -3, 96, false, false)]
    [InlineData(false, 3, 96, true, false)]
    [InlineData(false, 3, 96, false, true)]
    [InlineData(true, 3, 96, false, true)]
    public async Task DropShadow_Should_Not_Clip_Content_Outside_Layout_Bounds(
        bool effectOnParent, double offset, double dpi, bool clipToBounds, bool useVisualBrush)
    {
        var effect = new DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 3,
            OffsetX = offset,
            OffsetY = offset
        };
        var line = new Line
        {
            StartPoint = new Point(50, 50),
            EndPoint = new Point(250, 250),
            Stroke = Brushes.Red,
            StrokeThickness = 30,
            StrokeLineCap = PenLineCap.Round,
            Effect = effectOnParent ? null : effect
        };
        Control target = new Canvas
        {
            Width = 300 * dpi / 96,
            Height = 300 * dpi / 96,
            Background = Brushes.White,
            Children =
            {
                new Canvas
                {
                    Width = clipToBounds ? 250 : double.NaN,
                    Height = clipToBounds ? 250 : double.NaN,
                    ClipToBounds = clipToBounds,
                    Effect = effectOnParent ? effect : null,
                    Children = { line }
                }
            }
        };
        if (useVisualBrush)
        {
            target = new Border
            {
                Width = target.Width,
                Height = target.Height,
                Background = new VisualBrush(target)
            };
        }
        var testName = $"{nameof(DropShadow_Should_Not_Clip_Content_Outside_Layout_Bounds)}_{effectOnParent}_{offset}_{dpi}_{clipToBounds}_{useVisualBrush}";

        await RenderToFile(target, testName, dpi);

        using var immediate = SixLabors.ImageSharp.Image.Load<Rgba32>(
            Path.Combine(OutputPath, testName + ".immediate.out.png"));
        using var composited = SixLabors.ImageSharp.Image.Load<Rgba32>(
            Path.Combine(OutputPath, testName + ".composited.out.png"));
        var error = TestRenderHelper.CompareImages(immediate, composited);
        Assert.True(error < 0.001, $"Bitmap and compositor differ: {error}");
    }

    [Fact]
    public async Task RenderTargetBitmap_DropShadowEffect()
    {
        var root = new Grid
        {
            Width = 300,
            Height = 300,
            Children =
            {
                new Canvas
                {
                    Width = 300,
                    Height = 300,
                    Background = Brushes.White,
                    Children =
                    {
                        new Rectangle
                        {
                            Fill = Brushes.Red,
                            Width = 200,
                            Height = 200,
                            Margin = new Thickness(50),
                            Effect = new DropShadowEffect
                            {
                                Color = Colors.Black,
                                BlurRadius = 30
                            }
                        }
                    }
                }
            }
        };

        await RenderToFile(root);
        CompareImages();
    }
}
#endif
