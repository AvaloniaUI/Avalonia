using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public class InputExtensionsTests
{
    [Fact]
    public void InputHitTest_Should_Use_Coordinates_Relative_To_The_Subtree_Root()
    {
        Border target;
        using var services = new CompositorTestServices(new Size(200, 200))
        {
            TopLevel =
            {
                Content = new StackPanel
                {
                    Background = Brushes.White,
                    Children =
                    {
                        new Border
                        {
                            Width = 100,
                            Height = 200,
                            Background = Brushes.Red,
                        },
                        (target = new Border
                        {
                            Width = 100,
                            Height = 200,
                            Background = Brushes.Green,
                        })
                    },
                    Orientation = Orientation.Horizontal,
                }
            }
        };

        services.RunJobs();

        var result = target.InputHitTest(new Point(50, 50), enabledElementsOnly: false);

        Assert.Same(target, result);
    }
}
