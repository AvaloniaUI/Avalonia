using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Headless.UnitTests;

public class RenderingTests
{
    [Fact]
    public void Should_Render_Last_Frame_To_Bitmap()
    {
        var window = new Window
        {
            Content = new ContentControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(4),
                Content = new PathIcon
                {
                    Data = StreamGeometry.Parse("M0,9 L10,0 20,9 19,10 10,2 1,10 z")
                }
            },
            SizeToContent = SizeToContent.WidthAndHeight
        };
        window.Show();

        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
    }
}
