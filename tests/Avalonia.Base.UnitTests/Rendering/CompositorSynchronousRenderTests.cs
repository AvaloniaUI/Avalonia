using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

/// <summary>
/// Synchronous commits rendered on the UI thread (macOS) must not draw unrelated top levels:
/// drawing another window there can block the UI thread on that window's swapchain.
/// </summary>
public class CompositorSynchronousRenderTests : CompositorTestsBase
{
    [Fact]
    public void Paint_Draws_Only_The_Painted_TopLevel()
    {
        using var services = new CompositorTestServices(new Size(100, 100));
        var border = new Border { Background = Brushes.Red };
        var other = CreateTopLevel(services, border, out var otherEvents);

        border.Background = Brushes.Green;
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
        services.Renderer.Paint(new Rect(0, 0, 100, 100));

        Assert.NotEqual(0, services.Events.VisitedVisuals);
        Assert.Equal(0, otherEvents.VisitedVisuals);

        services.Timer.TriggerTick();

        Assert.NotEqual(0, otherEvents.VisitedVisuals);
        other.Dispose();
    }

    [Fact]
    public void Disposing_A_TopLevel_Does_Not_Draw_Other_TopLevels()
    {
        using var services = new CompositorTestServices(new Size(100, 100));
        var border = new Border { Background = Brushes.Red };
        services.TopLevel.Content = border;
        var other = CreateTopLevel(services, new Border(), out _);

        border.Background = Brushes.Green;
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
        other.Dispose();

        Assert.Equal(0, services.Events.VisitedVisuals);

        services.Timer.TriggerTick();

        Assert.NotEqual(0, services.Events.VisitedVisuals);
    }

    [Fact]
    public void Render_Loop_Tick_Does_Not_Wait_For_A_Synchronous_Render_On_The_UI_Thread()
    {
        using var services = new CompositorTestServices(new Size(100, 100));
        var renderLoopTask = (IRenderLoopTask)services.Compositor.Server;
        bool? tickReturned = null;

        // Runs under the compositor lock, inside the UI thread's synchronous render
        services.Compositor.PostServerJob(() =>
            tickReturned = Task.Run(renderLoopTask.Render).Wait(TimeSpan.FromSeconds(5)));
        services.Renderer.Paint(new Rect(0, 0, 100, 100));

        Assert.True(tickReturned);
    }

    private static EmbeddableControlRoot CreateTopLevel(CompositorTestServices services, Control content,
        out CompositorTestServices.DebugEvents events)
    {
        var topLevel = new EmbeddableControlRoot(new CompositorTestServices.TopLevelImpl(services.Compositor,
            new Size(100, 100)))
        {
            Content = content,
            Template = new FuncControlTemplate((_, scope) =>
            {
                var presenter = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
                };
                scope.Register("PART_ContentPresenter", presenter);
                return presenter;
            })
        };
        topLevel.Prepare();
        topLevel.StartRendering();
        services.RunJobs();

        events = new CompositorTestServices.DebugEvents();
        topLevel.Renderer.CompositionTarget.Server.DebugEvents = events;
        services.Events.Reset();
        return topLevel;
    }
}
