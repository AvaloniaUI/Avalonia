using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

/// <summary>
/// Composition brushes inside compositor-bound <see cref="DrawingRecording"/>s:
/// the recording's server render data observes every server resource in its
/// stream, so a brush change must invalidate the visuals displaying the
/// recording without any re-recording - the same chain that tracks mutable
/// Media brushes.
/// </summary>
public class CompositionBrushRecordingTests : ScopedTestBase
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

    /// <summary>
    /// Attaches a recording visual under a host control, settles the tree, and
    /// clears the dirty-rect capture so the next render reflects only the
    /// change under test.
    /// </summary>
    private CompositionRecordingVisual AttachAndSettle(DrawingRecording recording)
    {
        var visual = _services.Compositor.CreateRecordingVisual();
        visual.Recording = recording;

        var host = new Control { Width = 100, Height = 100 };
        _services.TopLevel.Content = host;
        _services.RunJobs();
        ElementComposition.SetElementChildVisual(host, visual);
        _services.RunJobs();
        ForceCommitAndRender();

        // Steady state: with nothing changed, a frame produces no dirty rects.
        _services.Events.Reset();
        ForceCommitAndRender();
        Assert.Empty(_services.Events.Rects);

        return visual;
    }

    [Fact]
    public void Composition_Brush_Change_Should_Invalidate_A_Recording_Visual()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(brush, null, new Rect(10, 10, 50, 50)));
        AttachAndSettle(recording);

        brush.Color = Colors.Blue;
        ForceCommitAndRender();

        Assert.NotEmpty(_services.Events.Rects);
        recording.Dispose();
    }

    [Fact]
    public void Server_Side_Composition_Brush_Change_Should_Invalidate_A_Recording_Visual()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(brush, null, new Rect(10, 10, 50, 50)));
        AttachAndSettle(recording);

        // A value produced on the render thread must invalidate through the
        // same observer chain, with no client serialization involved. The
        // animation evaluator writes the field and raises
        // NotifyAnimatedValueChanged, so the test does exactly that.
        var server = (ServerCompositionSolidColorBrush)brush.Server;
        server.Color = Colors.Lime;
        server.NotifyAnimatedValueChanged(ServerCompositionSolidColorBrush.s_IdOfColorProperty);
        _services.Compositor.Server.Render(false);

        Assert.NotEmpty(_services.Events.Rects);
        recording.Dispose();
    }

    [Fact]
    public void Composition_Gradient_Stop_Change_Should_Invalidate_A_Recording_Visual()
    {
        var stop = _services.Compositor.CreateGradientStop(0, Colors.Red);
        var brush = _services.Compositor.CreateLinearGradientBrush();
        brush.GradientStops.Add(stop);
        brush.GradientStops.Add(_services.Compositor.CreateGradientStop(1, Colors.Blue));

        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(brush, null, new Rect(10, 10, 50, 50)));
        AttachAndSettle(recording);

        // Stop -> gradient brush -> render data -> visual.
        stop.Color = Colors.Yellow;
        ForceCommitAndRender();

        Assert.NotEmpty(_services.Events.Rects);
        recording.Dispose();
    }

    [Fact]
    public void Composition_Brush_Change_Should_Propagate_Through_A_Nested_Recording()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);
        var inner = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(brush, null, new Rect(0, 0, 40, 40)));
        var outer = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRecording(inner));
        AttachAndSettle(outer);

        // Brush -> inner render data -> outer render data -> visual: the outer
        // stream's resource table holds the inner render data, so observation
        // is transitive.
        brush.Color = Colors.Blue;
        ForceCommitAndRender();

        Assert.NotEmpty(_services.Events.Rects);
        outer.Dispose();
        inner.Dispose();
    }

    [Fact]
    public void Composition_Brush_On_A_Pen_Should_Invalidate_A_Recording_Visual()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);
        var pen = new Pen(brush, 2);
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawLine(pen, new Point(0, 0), new Point(50, 50)));
        AttachAndSettle(recording);

        // Brush -> server pen resource -> render data -> visual.
        brush.Color = Colors.Blue;
        ForceCommitAndRender();

        Assert.NotEmpty(_services.Events.Rects);
        recording.Dispose();
    }

    [Fact]
    public void Composition_Brush_In_Recording_Brush_Content_Should_Invalidate_The_Consumer()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);
        var content = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(brush, null, new Rect(0, 0, 20, 20)));

        var recordingBrush = new DrawingRecordingBrush(content) { TileMode = TileMode.Tile };
        var consumer = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(recordingBrush, null, new Rect(0, 0, 80, 80)));
        AttachAndSettle(consumer);

        // Brush -> content render data -> scene-brush content render data ->
        // server content brush -> consumer render data -> visual.
        brush.Color = Colors.Blue;
        ForceCommitAndRender();

        Assert.NotEmpty(_services.Events.Rects);
        consumer.Dispose();
        content.Dispose();
    }

    [Fact]
    public void Composition_Brush_Change_Should_Keep_Bounds_And_Not_Raise_BoundsChanged()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(brush, null, new Rect(10, 10, 50, 50)));
        AttachAndSettle(recording);

        var boundsBefore = recording.Bounds;
        var boundsChangedRaised = false;
        recording.BoundsChanged += (_, _) => boundsChangedRaised = true;

        brush.Color = Colors.Blue;
        _services.RunJobs();

        Assert.Equal(boundsBefore, recording.Bounds);
        Assert.False(boundsChangedRaised);
        recording.Dispose();
    }

    [Fact]
    public void Immutable_Create_Should_Reject_A_Composition_Brush()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DrawingRecording.Create(ctx =>
                ctx.DrawRectangle(brush, null, new Rect(0, 0, 10, 10))));

        // The message should point at the overload that can host the brush.
        Assert.Contains("Compositor", exception.Message);
    }

    [Fact]
    public void Disposing_The_Brush_Before_The_Recording_Should_Not_Throw_On_Render()
    {
        var brush = _services.Compositor.CreateSolidColorBrush(Colors.Red);
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(brush, null, new Rect(10, 10, 50, 50)));
        AttachAndSettle(recording);

        brush.Dispose();
        ForceCommitAndRender();

        recording.Dispose();
        ForceCommitAndRender();
    }
}
