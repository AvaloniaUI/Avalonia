using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class BugRepros() : TestBase(nameof(BugRepros))
{
    [Fact]
    public async Task Sibling_Visuals_With_Opacity_Should_Not_Affect_Each_Other()
    {
        var brushes = new IBrush[]
        {
            Brushes.Red,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Yellow,
            Brushes.Magenta,
            Brushes.Cyan,
            Brushes.Orange,
            Brushes.Purple,
            Brushes.Pink,
            Brushes.Brown
        };

        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 300,
            Height = 500,
            Background = Brushes.White,
        };

        for (int i = 0; i < brushes.Length; i++)
        {
            var border = new Border
            {
                Width = 280,
                Height = 40,
                BorderThickness = new Thickness(2),
                Margin = new Thickness(5),
                Background = brushes[i],
                Opacity = 0.3
            };
            stackPanel.Children.Add(border);
        }

        await RenderToFile(stackPanel);
        CompareImages();
    }
}
