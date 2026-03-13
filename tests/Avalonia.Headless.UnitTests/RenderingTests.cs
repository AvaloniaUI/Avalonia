using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class RenderingTests
{
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
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

        AssertHelper.NotNull(frame);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
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

        AssertHelper.NotNull(frame);
    }
    
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
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

        AssertHelper.NotNull(frame);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
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
        AssertHelper.NotNull(frame);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public async Task Should_Render_To_A_Compositor_Snapshot_Capture()
    {
        var window = new Window
        {
            Content = new ContentControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Width = 100,
                Height = 100,
                Background = Brushes.Green
            },
            SizeToContent = SizeToContent.WidthAndHeight
        };

        window.Show();

        Dispatcher.UIThread.RunJobs();

        var compositionVisual = ElementComposition.GetElementVisual(window)!;
        var snapshot = await compositionVisual.Compositor.CreateCompositionVisualSnapshot(compositionVisual, 1);

        AssertHelper.NotNull(snapshot);
        AssertHelper.Equal(100, snapshot.Size.Width);
        AssertHelper.Equal(100, snapshot.Size.Height);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Should_Change_Render_Scaling()
    {
        var window = new Window
        {
            Content = new Border
            {
                Width = 100,
                Height = 100,
                Background = Brushes.Red
            }
        };

        window.Show();

        var frameBefore = window.CaptureRenderedFrame();
        AssertHelper.NotNull(frameBefore);

        var sizeBefore = frameBefore!.PixelSize;

        window.SetRenderScaling(2.0);

        AssertHelper.Equal(2.0, window.RenderScaling);

        var frameAfter = window.CaptureRenderedFrame();
        AssertHelper.NotNull(frameAfter);

        var sizeAfter = frameAfter!.PixelSize;

        AssertHelper.Equal(sizeBefore.Width * 2, sizeAfter.Width);
        AssertHelper.Equal(sizeBefore.Height * 2, sizeAfter.Height);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Should_Keep_Client_Size_After_Scaling_Change()
    {
        var window = new Window
        {
            Width = 200,
            Height = 150
        };

        window.Show();
        window.CaptureRenderedFrame();

        var clientSizeBefore = window.ClientSize;

        window.SetRenderScaling(2.0);
        window.CaptureRenderedFrame();

        AssertHelper.Equal(clientSizeBefore.Width, window.ClientSize.Width);
        AssertHelper.Equal(clientSizeBefore.Height, window.ClientSize.Height);
    }
}
