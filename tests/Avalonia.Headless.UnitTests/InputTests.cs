using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Headless.XUnit.Tests;

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

        Dispatcher.UIThread.RunJobs();

        ((IHeadlessWindow)window.PlatformImpl!).MouseDown(new Point(50, 50), 0);
        ((IHeadlessWindow)window.PlatformImpl!).MouseUp(new Point(50, 50), 0);
        
        Assert.True(buttonClicked);
    }
}
