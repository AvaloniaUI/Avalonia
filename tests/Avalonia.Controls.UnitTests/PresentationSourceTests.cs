using System;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public sealed class PresentationSourceTests : ScopedTestBase
{
    [Fact]
    public void ChromeHitTest_Prefers_Overlay_Over_Content()
    {
        var overlay = new Border
        {
            Background = Brushes.Red,
            [WindowDecorationProperties.ElementRoleProperty] = WindowDecorationsElementRole.TitleBar
        };

        var content = new Border
        {
            Background = Brushes.Blue
        };

        DoChromeHitTest(
            underlay: null,
            content: content,
            overlay: overlay,
            expectedChromeVisual: overlay,
            expectedRole: WindowDecorationsElementRole.TitleBar);
    }

    [Fact]
    public void ChromeHitTest_Prefers_Content_Over_Underlay()
    {
        var underlay = new Border
        {
            Background = Brushes.Red,
            [WindowDecorationProperties.ElementRoleProperty] = WindowDecorationsElementRole.TitleBar
        };

        var content = new Border
        {
            Background = Brushes.Blue
        };

        DoChromeHitTest(
            underlay: underlay,
            content: content,
            overlay: null,
            expectedChromeVisual: content,
            expectedRole: null);
    }

    private static void DoChromeHitTest(
        Control? underlay,
        Control? content,
        Control? overlay,
        Visual? expectedChromeVisual,
        WindowDecorationsElementRole? expectedRole)
    {
        const double width = 100;
        const double height = 100;

        if (underlay is not null)
        {
            underlay.Width = width;
            underlay.Height = height;
        }

        if (content is not null)
        {
            content.Width = width;
            content.Height = height;
        }

        if (overlay is not null)
        {
            overlay.Width = width;
            overlay.Height = height;
        }

        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var decorations = new WindowDrawnDecorationsContent
        {
            Underlay = underlay,
            Overlay = overlay
        };

        Application.Current!.Resources.Add(typeof(WindowDrawnDecorations), CreateDecorationsTheme(decorations));

        var renderTimer = new CompositorTestServices.ManualRenderTimer();
        var compositor = RendererMocks.CreateDummyCompositor(renderTimer);
        var windowImpl = MockWindowingPlatform.CreateWindowMock(width, height, compositor);
        windowImpl.Setup(w => w.IsClientAreaExtendedToDecorations).Returns(true);
        windowImpl.Setup(w => w.RequestedDrawnDecorations).Returns(PlatformRequestedDrawnDecoration.TitleBar);
        windowImpl.Setup(w => w.NeedsManagedDecorations).Returns(true);

        var window = new Window(windowImpl.Object)
        {
            WindowDecorations = WindowDecorations.Full,
            ExtendClientAreaToDecorationsHint = true,
            Content = content
        };

        window.Show();

        Dispatcher.CurrentDispatcher.RunJobs(null, TestContext.Current.CancellationToken);
        renderTimer.TriggerTick();
        Dispatcher.CurrentDispatcher.RunJobs(null, TestContext.Current.CancellationToken);

        var hitTestPoint = new Point(width / 2, height / 2);

        var clientVisual = window.GetVisualAt(hitTestPoint);
        Assert.Same(window.Content, clientVisual);

        var chromeVisual = window.PresentationSource.RootVisual.GetVisualAt(hitTestPoint);
        Assert.Same(expectedChromeVisual, chromeVisual);

        var chromeRole = ((IInputRoot)window.PresentationSource).HitTestChromeElement(hitTestPoint);
        Assert.Equal(expectedRole, chromeRole);
    }

    private static ControlTheme CreateDecorationsTheme(WindowDrawnDecorationsContent content)
    {
        var template = new WindowDrawnDecorationsTemplate
        {
            Content = (IServiceProvider? _) => new TemplateResult<WindowDrawnDecorationsContent>(content, new NameScope())
        };

        return new ControlTheme(typeof(WindowDrawnDecorations))
        {
            Setters =
            {
                new Setter(WindowDrawnDecorations.TemplateProperty, template)
            }
        };
    }
}
