using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositorBoundDrawingRecordingTests : ScopedTestBase
{
    private readonly CompositorTestServices _services = new();

    public override void Dispose()
    {
        _services.Dispose();
        base.Dispose();
    }

    private void ForceCommitAndRender()
    {
        _services.Compositor.Commit();
        _services.Compositor.Server.Render(false);
    }

    [Fact]
    public void Create_With_Compositor_Returns_NonNull()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        Assert.NotNull(recording);
        Assert.False(recording.IsDisposed);
        Assert.Same(_services.Compositor, recording.Compositor);
        recording.Dispose();
    }

    [Fact]
    public void Compositor_Bound_Bounds_Available_After_Commit()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        ForceCommitAndRender();

        var bounds = recording.Bounds;
        Assert.Equal(new Rect(10, 10, 100, 50), bounds);
        recording.Dispose();
    }

    [Fact]
    public void Compositor_Bound_HitTest_Works()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        Assert.True(recording.HitTest(new Point(50, 30)));
        Assert.False(recording.HitTest(new Point(0, 0)));
        recording.Dispose();
    }

    [Fact]
    public void Compositor_Bound_Supports_Mutable_Brush()
    {
        var brush = new SolidColorBrush(Colors.Red);
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(brush, null, new Rect(0, 0, 100, 100));
        });

        Assert.NotNull(recording);
        recording.Dispose();
    }

    [Fact]
    public void Compositor_Bound_DrawRecording_Into_RenderDataContext()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 50, 50));
        });

        using var outerCtx = new RenderDataDrawingContext(_services.Compositor);
        outerCtx.DrawRecording(recording);
        var result = outerCtx.GetRenderResults();

        Assert.NotNull(result);
        recording.Dispose();
    }

    [Fact]
    public void Disposed_Compositor_Bound_Recording_Throws()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        recording.Dispose();
        Assert.True(recording.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => recording.Bounds);
    }

    [Fact]
    public void Immutable_Recording_Renders_Via_PlatformDrawingContext()
    {
        var mockImpl = new Mock<IDrawingContextImpl>();
        mockImpl.Setup(x => x.Transform).Returns(Matrix.Identity);

        var recording = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 100, 50));
        });

        using var platformCtx = new PlatformDrawingContext(mockImpl.Object, false);
        platformCtx.DrawRecording(recording);

        mockImpl.Verify(x => x.DrawRectangle(
            It.IsAny<IBrush>(), It.IsAny<IPen>(),
            It.IsAny<RoundedRect>(), It.IsAny<BoxShadows>()), Times.Once);
    }

    [Fact]
    public void Compositor_Bound_Recording_Renders_Via_PlatformDrawingContext()
    {
        var mockImpl = new Mock<IDrawingContextImpl>();
        mockImpl.Setup(x => x.Transform).Returns(Matrix.Identity);

        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 100, 50));
        });

        using var platformCtx = new PlatformDrawingContext(mockImpl.Object, false);
        platformCtx.DrawRecording(recording);

        mockImpl.Verify(x => x.DrawRectangle(
            It.IsAny<IBrush>(), It.IsAny<IPen>(),
            It.IsAny<RoundedRect>(), It.IsAny<BoxShadows>()), Times.Once);

        recording.Dispose();
    }

    [Fact]
    public void Immutable_Recording_Embeds_In_Compositor_Context()
    {
        var recording = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 50, 50));
        });

        using var outerCtx = new RenderDataDrawingContext(_services.Compositor);
        outerCtx.DrawRecording(recording);
        var result = outerCtx.GetRenderResults();

        Assert.NotNull(result);
    }
}
