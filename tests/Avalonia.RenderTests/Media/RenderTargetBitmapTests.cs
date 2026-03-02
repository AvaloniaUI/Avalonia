using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class RenderTargetBitmapTests : TestBase
{
    public RenderTargetBitmapTests() : base(@"Media\RenderTargetBitmap")
    {
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

        // var rtb = new RenderTargetBitmap(new PixelSize(300,300), new Vector(96,96));
        // rtb.Render(root);
        //
        // rtb.Save("test.png");
    }
}
