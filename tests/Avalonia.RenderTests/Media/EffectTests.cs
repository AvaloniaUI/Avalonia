using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;
#pragma warning disable CS0649

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;

public class EffectTests : TestBase
{
    public EffectTests() : base(@"Media\Effects")
    {
    }

    [Fact]
    public async Task DropShadowEffect()
    {
        var target = new Border
        {
            Width = 200,
            Height = 200,
            Background = Brushes.White,
            Child = new Border()
            {
                Background = null,
                Margin = new Thickness(40),
                Effect = new ImmutableDropShadowEffect(20, 30, 5, Colors.Green, 1),
                Child = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)),
                    BorderBrush = Brushes.Red,
                    BorderThickness = new Thickness(5)
                }
            }
        };
        
        await RenderToFile(target);
        CompareImages(skipImmediate: true);
    }

    [Fact]
    public async Task EffectFollowedByNonEffect()
    {
        var target = new Border
        {
            Background = Brushes.White,
            Width = 200,
            Height = 200,
            Child = new Panel
            {
                Margin = new Thickness(25),
                Children =
                {
                    new Rectangle
                    {
                        Fill = Brushes.Yellow,
                        Effect = new DropShadowEffect
                        {
                            Opacity = 1,
                            OffsetX = 0,
                            OffsetY = 0,
                            Color = Colors.Black,
                            BlurRadius = 50
                        }
                    },
                    new Rectangle
                    {
                        Fill = new SolidColorBrush(0x7F007FFF)
                    }
                }
            }
        };

        await RenderToFile(target);
        CompareImages(skipImmediate: true);
    }
    
}
#endif
