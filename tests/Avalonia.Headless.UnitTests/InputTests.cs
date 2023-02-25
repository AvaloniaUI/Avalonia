using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Headless.UnitTests;

public class InputTests
{
    [Fact]
    public void Should_Click_Button_On_Window()
    {
        var buttonClicked = false;
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        button.Click += (_, _) => buttonClicked = true;

        var window = new Window
        {
            Width = 100,
            Height = 100,
            Content = button
        };
        window.Show();

        window.MouseDown(new Point(50, 50), MouseButton.Left);
        window.MouseUp(new Point(50, 50), MouseButton.Left);
        
        Assert.True(buttonClicked);
    }
}
