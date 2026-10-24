using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public sealed class TopLevelPaintTests : ScopedTestBase
{
    [Fact]
    public void Paint_With_FlushRenderPass_Runs_Pending_Layout_Without_Running_Dispatcher_Jobs()
    {
        using var services = new CompositorTestServices();
        var content = new Border { Width = 100, Height = 100 };
        services.TopLevel.Content = content;
        services.RunJobs();

        var jobRan = false;
        content.Width = 200;
        Dispatcher.UIThread.Post(() => jobRan = true);

        Paint(services, new ITopLevelImpl.PaintOptions { FlushRenderPass = true });

        Assert.Equal(200, content.Bounds.Width);
        Assert.False(jobRan);

        services.RunJobs();
        Assert.True(jobRan);
    }

    [Fact]
    public void Default_Paint_Does_Not_Run_Pending_Render_Pass()
    {
        using var services = new CompositorTestServices();
        var content = new Border { Width = 100, Height = 100 };
        services.TopLevel.Content = content;
        services.RunJobs();

        content.Width = 200;

        Paint(services, null);

        Assert.Equal(100, content.Bounds.Width);

        services.RunJobs();
        Assert.Equal(200, content.Bounds.Width);
    }

    [Fact]
    public void Paint_With_FlushRenderPass_Disables_Dispatcher_Processing_During_Layout()
    {
        using var services = new CompositorTestServices();
        var content = new MeasureCallbackControl();
        services.TopLevel.Content = content;
        services.RunJobs();

        Exception? exception = null;
        content.OnMeasure = () => exception = Record.Exception(() => Dispatcher.UIThread.RunJobs());
        content.InvalidateMeasure();

        Paint(services, new ITopLevelImpl.PaintOptions { FlushRenderPass = true });

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Paint_With_FlushRenderPass_From_Layout_Pass_Does_Not_Run_Render_Callbacks()
    {
        using var services = new CompositorTestServices();
        var content = new MeasureCallbackControl();
        services.TopLevel.Content = content;
        services.RunJobs();

        var painted = false;
        var inPaint = false;
        var callbackRanInPaint = false;
        Exception? exception = null;
        content.OnMeasure = () =>
        {
            if (painted)
                return;
            painted = true;
            MediaContext.Instance.BeginInvokeOnRender(() => callbackRanInPaint = inPaint);
            inPaint = true;
            exception = Record.Exception(() => Paint(services, new ITopLevelImpl.PaintOptions { FlushRenderPass = true }));
            inPaint = false;
        };
        content.InvalidateMeasure();
        services.RunJobs();

        Assert.True(painted);
        Assert.Null(exception);
        Assert.False(callbackRanInPaint);
    }

    [Fact]
    public void Paint_With_FlushRenderPass_Runs_Due_Animation_Frame_Once()
    {
        using var services = new CompositorTestServices();
        services.RunJobs();

        var frames = 0;
        MediaContext.Instance.RequestAnimationFrame(_ => frames++);

        Paint(services, new ITopLevelImpl.PaintOptions { FlushRenderPass = true });
        Assert.Equal(1, frames);

        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, frames);
    }

    [Fact]
    public void Paint_With_FlushRenderPass_Does_Not_Pulse_Animations_Without_Pending_Pass()
    {
        using var services = new CompositorTestServices();
        var pulses = 0;
        using var _ = MediaContext.Instance.Clock.Subscribe(new AnonymousObserver<TimeSpan>(_ => pulses++));
        services.RunJobs();
        var pulsesBefore = pulses;

        Paint(services, new ITopLevelImpl.PaintOptions { FlushRenderPass = true });

        Assert.Equal(pulsesBefore, pulses);
    }

    private static void Paint(CompositorTestServices services, ITopLevelImpl.PaintOptions? options) =>
        ((CompositorTestServices.TopLevelImpl)services.TopLevel.PlatformImpl!).Paint!(options);

    private class MeasureCallbackControl : Decorator
    {
        public Action? OnMeasure { get; set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            OnMeasure?.Invoke();
            return base.MeasureOverride(availableSize);
        }
    }
}
