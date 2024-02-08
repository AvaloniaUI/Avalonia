using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class RenderingTests
{
#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
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

        var frame = window.CaptureRenderedFrame();

        Assert.NotNull(frame);
    }
    
#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    public void Should_Not_Crash_On_GeometryGroup()
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
                    Data = new GeometryGroup()
                    {
                        Children = new GeometryCollection(new []
                        {
                            new RectangleGeometry(new Rect(0, 0, 50, 50)),
                            new RectangleGeometry(new Rect(50, 50, 100, 100))
                        })
                    }
                }
            },
            SizeToContent = SizeToContent.WidthAndHeight
        };

        window.Show();

        var frame = window.CaptureRenderedFrame();

        Assert.NotNull(frame);
    }
    
#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    public void Should_Not_Crash_On_CombinedGeometry()
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
                    Data = new CombinedGeometry(GeometryCombineMode.Union,
                        new RectangleGeometry(new Rect(0, 0, 50, 50)),
                        new RectangleGeometry(new Rect(50, 50, 100, 100)))
                }
            },
            SizeToContent = SizeToContent.WidthAndHeight
        };

        window.Show();

        var frame = window.CaptureRenderedFrame();

        Assert.NotNull(frame);
    }

#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    public void Should_Not_Hang_With_Non_Trivial_Layout()
    {
        var window = new Window
        {
            Content = new ContentControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(1),
                Content = new ListBox
                {
                    ItemsSource = new ObservableCollection<string>()
                    {
                        "Test 1",
                        "Test 2"
                    }
                }
            },
            SizeToContent = SizeToContent.WidthAndHeight
        };

        window.Show();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
    }
}
