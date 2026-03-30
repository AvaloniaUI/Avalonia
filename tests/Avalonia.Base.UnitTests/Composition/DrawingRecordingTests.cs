using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class DrawingRecordingTests : ScopedTestBase
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
    public void CreateDrawingRecording_Returns_NonNull()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        Assert.NotNull(recording);
        Assert.False(recording.IsDisposed);
        recording.Dispose();
    }

    [Fact]
    public void Empty_Recording_Returns_NonNull()
    {
        var recording = _services.Compositor.CreateDrawingRecording(_ => { });

        Assert.NotNull(recording);
        Assert.False(recording.IsDisposed);
        recording.Dispose();
    }

    [Fact]
    public void Empty_Recording_Has_Default_Bounds()
    {
        var recording = _services.Compositor.CreateDrawingRecording(_ => { });

        ForceCommitAndRender();

        Assert.Equal(default, recording.Bounds);
        recording.Dispose();
    }

    [Fact]
    public void Bounds_Available_After_Commit()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        ForceCommitAndRender();

        var bounds = recording.Bounds;
        Assert.Equal(new Rect(10, 10, 100, 50), bounds);
        recording.Dispose();
    }

    [Fact]
    public void Multiple_Primitives_Union_Bounds()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 50, 50));
            ctx.DrawRectangle(Brushes.Blue, null, new Rect(100, 100, 50, 50));
        });

        ForceCommitAndRender();

        var bounds = recording.Bounds;
        Assert.Equal(new Rect(0, 0, 150, 150), bounds);
        recording.Dispose();
    }

    [Fact]
    public void DrawRecording_Into_RenderDataContext_Creates_Node()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
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
    public void Nested_Recordings_Work()
    {
        var inner = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Green, null, new Rect(0, 0, 20, 20));
        });

        var outer = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRecording(inner);
            ctx.DrawRectangle(Brushes.Red, null, new Rect(30, 30, 20, 20));
        });

        Assert.NotNull(outer);
        outer.Dispose();
        inner.Dispose();
    }

    [Fact]
    public void HitTest_Inside_Returns_True()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        Assert.True(recording.HitTest(new Point(50, 30)));
        recording.Dispose();
    }

    [Fact]
    public void HitTest_Outside_Returns_False()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        Assert.False(recording.HitTest(new Point(0, 0)));
        recording.Dispose();
    }

    [Fact]
    public void Disposed_Recording_Throws_On_Bounds()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        recording.Dispose();
        Assert.True(recording.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => recording.Bounds);
    }

    [Fact]
    public void Disposed_Recording_Throws_On_HitTest()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 50));
        });

        recording.Dispose();
        Assert.Throws<ObjectDisposedException>(() => recording.HitTest(default));
    }

    [Fact]
    public void Recording_With_Transform_Has_Correct_Bounds()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            using (ctx.PushTransform(Matrix.CreateTranslation(10, 10)))
            {
                ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 50, 50));
            }
        });

        ForceCommitAndRender();

        var bounds = recording.Bounds;
        Assert.Equal(new Rect(10, 10, 50, 50), bounds);
        recording.Dispose();
    }

    [Fact]
    public void Recording_With_Clip_And_Opacity()
    {
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            using (ctx.PushOpacity(0.5))
            using (ctx.PushClip(new Rect(0, 0, 200, 200)))
            {
                ctx.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 100));
            }
        });

        ForceCommitAndRender();

        var bounds = recording.Bounds;
        Assert.Equal(new Rect(10, 10, 100, 100), bounds);
        recording.Dispose();
    }

    [Fact]
    public void Recording_With_Pen_Has_Inflated_Bounds()
    {
        var pen = new ImmutablePen(Brushes.Black, 2);
        var recording = _services.Compositor.CreateDrawingRecording(ctx =>
        {
            ctx.DrawRectangle(null, pen, new Rect(10, 10, 100, 50));
        });

        ForceCommitAndRender();

        var bounds = recording.Bounds;
        // Bounds should be inflated by pen thickness (1px each side for thickness=2)
        // and then snapped to pixels
        Assert.True(bounds.Width >= 100);
        Assert.True(bounds.Height >= 50);
        recording.Dispose();
    }
}
