using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Layout;
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

    [Fact]
    public async Task DropShadowEffect_On_TextBlock_With_Background()
    {
        var target = new Border
        {
            Background = Brushes.White,
            Width = 260,
            Height = 180,
            Padding = new Thickness(30),
            Child = new TextBlock
            {
                Text = "Test",
                Background = Brushes.LightBlue,
                FontSize = 64,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.MidnightBlue,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 18,
                    Color = Colors.Black,
                    Opacity = 0.75,
                    OffsetX = 14,
                    OffsetY = 14
                }
            }
        };

        await RenderToFile(target);
        CompareImages(skipImmediate: true);
    }

    [Fact]
    public async Task DropShadowEffect_On_Border_With_ClipToBounds()
    {
        var target = new Border
        {
            Background = Brushes.White,
            Width = 260,
            Height = 180,
            Padding = new Thickness(30),
            Child = new Border
            {
                Width = 120,
                Height = 70,
                Background = Brushes.LightBlue,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 18,
                    Color = Colors.Black,
                    Opacity = 0.75,
                    OffsetX = 14,
                    OffsetY = 14
                }
            }
        };

        await RenderToFile(target);
        CompareImages(skipImmediate: true);
    }

    [Fact]
    public async Task DropShadowEffect_On_Border_With_GeometryClip()
    {
        var target = new Border
        {
            Background = Brushes.White,
            Width = 260,
            Height = 220,
            Padding = new Thickness(40),
            Child = new Border
            {
                Width = 140,
                Height = 90,
                Background = Brushes.LightBlue,
                Clip = new EllipseGeometry(new Rect(0, 0, 140, 90)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 16,
                    Color = Colors.Black,
                    Opacity = 0.75,
                    OffsetX = 12,
                    OffsetY = 12
                }
            }
        };

        await RenderToFile(target);
        CompareImages();
    }

}
#endif
