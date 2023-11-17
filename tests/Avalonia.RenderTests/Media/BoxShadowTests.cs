using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Xunit;
#pragma warning disable CS0649

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;

public class BoxShadowTests : TestBase
{
    
    public BoxShadowTests() : base(@"Media\BoxShadow")
    {
    }
    
    [Fact]
    public async Task BoxShadowShouldBeRenderedEvenWithNullBrushAndPen()
    {
        var target = new Border
        {
            Width = 200,
            Height = 200,
            Background = null,
            Child = new Border()
            {
                Background = null,
                Margin = new Thickness(40),
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    Blur = 0,
                    Color = Colors.Blue,
                    OffsetX = 10,
                    OffsetY = 15,
                    Spread = 0
                }),
                Child = new Border
                {
                    Background = Brushes.Red
                }
            }
        };
        
        await RenderToFile(target);
        CompareImages();
    }


}

#endif