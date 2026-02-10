using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class ContentPresenterTests : TestBase
{
    public ContentPresenterTests()
        : base(@"Controls\ContentPresenter")
    {
    }

    [Fact]
    public async Task ContentPresenter_RoundedClip_Clips_Child()
    {
        Decorator target = new Decorator
        {
            Padding = new Thickness(8),
            Width = 320,
            Height = 200,
            Child = new ContentPresenter
            {
                Width = 200,
                Height = 100,
                Background = Brushes.Red,
                BorderBrush = Brushes.Green,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Content = new Border
                {
                    Width = 80,
                    Height = 100,
                    Background = Brushes.Blue
                }
            }
        };

        await RenderToFile(target);
        CompareImages();
    }
}
