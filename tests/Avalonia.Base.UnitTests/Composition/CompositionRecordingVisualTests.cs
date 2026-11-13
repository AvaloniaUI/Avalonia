using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositionRecordingVisualTests : ScopedTestBase
{
    private readonly CompositorTestServices _services = new();

    public override void Dispose()
    {
        _services.Dispose();
        base.Dispose();
    }

    private static LtrbRect? GetTransformedSubtreeBounds(CompositionVisual visual) =>
        (LtrbRect?)typeof(ServerCompositionVisual)
            .GetField("_transformedSubTreeBounds",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(visual.Server);

    [Fact]
    public void Recording_Reaches_The_Server_Visual()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(Brushes.Crimson, null, new Rect(20, 20, 60, 60)));

        var visual = _services.Compositor.CreateRecordingVisual();
        visual.Recording = recording;

        var host = new Control { Width = 100, Height = 100 };
        _services.TopLevel.Content = host;
        _services.RunJobs();
        ElementComposition.SetElementChildVisual(host, visual);
        _services.RunJobs();

        _services.Compositor.Commit();
        _services.Compositor.Server.Render(false);

        // The recording's render data deserialized into the server visual and
        // the visual is part of the server tree.
        var server = (ServerCompositionRecordingVisual)visual.Server;
        var contentBounds = server.ComputeOwnContentBounds();
        Assert.NotNull(contentBounds);
        Assert.Equal(20, contentBounds!.Value.Left);
        Assert.Equal(80, contentBounds.Value.Right);

        var elementVisual = (CompositionContainerVisual)ElementComposition.GetElementVisual(host)!;
        Assert.True(elementVisual.Children.Contains(visual), "child visual not in client children");
        Assert.True(server.Parent != null, "server visual not parented");
        Assert.True(visual.Root != null, "client visual has no root");

        // The bounds pass produced subtree bounds, so the render walk paints
        // the visual instead of culling it.
        var subtreeBounds = GetTransformedSubtreeBounds(visual);
        Assert.NotNull(subtreeBounds);
        Assert.Equal(20, subtreeBounds!.Value.Left);
        Assert.Equal(80, subtreeBounds.Value.Right);

        recording.Dispose();
    }

    [Fact]
    public void Assigning_A_Disposed_Recording_Throws()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(Brushes.Crimson, null, new Rect(0, 0, 50, 50)));
        recording.Dispose();

        var visual = _services.Compositor.CreateRecordingVisual();

        Assert.Throws<ObjectDisposedException>(() => visual.Recording = recording);
        Assert.Null(visual.Recording);
    }

    private (CompositionRecordingVisual Visual, Control Host) AttachRecordingVisual()
    {
        var visual = _services.Compositor.CreateRecordingVisual();
        var host = new Control { Width = 100, Height = 100 };
        _services.TopLevel.Content = host;
        _services.RunJobs();
        ElementComposition.SetElementChildVisual(host, visual);
        CommitAndRender();
        return (visual, host);
    }

    private void CommitAndRender()
    {
        _services.RunJobs();
        _services.Compositor.Commit();
        _services.Compositor.Server.Render(false);
    }

    private DrawingRecording RecordBitmap(IRef<IBitmapImpl> bitmap)
    {
        var rect = new Rect(0, 0, 50, 50);
        return DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Crimson, null, rect);
            ctx.DrawBitmap(bitmap, 1, rect, rect);
        });
    }

    [Fact]
    public void Visual_Keeps_A_Recording_Disposed_While_Assigned_Until_It_Is_Replaced()
    {
        var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());
        var recording = RecordBitmap(bitmap);
        var (visual, _) = AttachRecordingVisual();
        visual.Recording = recording;
        CommitAndRender();

        // The visual holds its own reference to the content it displays.
        recording.Dispose();
        CommitAndRender();
        Assert.Equal(2, bitmap.RefCount);
        Assert.True(visual.HitTest(new Point(10, 10)));

        visual.Recording = null;
        CommitAndRender();
        CommitAndRender();
        Assert.Equal(1, bitmap.RefCount);
        Assert.False(visual.HitTest(new Point(10, 10)));
    }

    [Fact]
    public void Visual_Releases_Its_Recording_When_Detached()
    {
        var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());
        var recording = RecordBitmap(bitmap);
        var (visual, host) = AttachRecordingVisual();
        visual.Recording = recording;
        CommitAndRender();

        recording.Dispose();
        CommitAndRender();
        Assert.Equal(2, bitmap.RefCount);

        // A visual that leaves the tree and is dropped must not keep the content alive.
        ElementComposition.SetElementChildVisual(host, null);
        CommitAndRender();
        CommitAndRender();
        Assert.Equal(1, bitmap.RefCount);
    }

    [Fact]
    public void Visual_Displays_Its_Recording_Again_When_Reattached()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(Brushes.Crimson, null, new Rect(20, 20, 60, 60)));
        var (visual, host) = AttachRecordingVisual();
        visual.Recording = recording;
        CommitAndRender();

        ElementComposition.SetElementChildVisual(host, null);
        CommitAndRender();
        ElementComposition.SetElementChildVisual(host, visual);
        CommitAndRender();

        var server = (ServerCompositionRecordingVisual)visual.Server;
        Assert.NotNull(server.ComputeOwnContentBounds());
        Assert.True(visual.HitTest(new Point(30, 30)));

        recording.Dispose();
    }

    [Fact]
    public void Recording_Disposed_While_Detached_Is_Not_Displayed_On_Reattach()
    {
        var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());
        var recording = RecordBitmap(bitmap);
        var (visual, host) = AttachRecordingVisual();
        visual.Recording = recording;
        CommitAndRender();

        ElementComposition.SetElementChildVisual(host, null);
        CommitAndRender();
        recording.Dispose();
        CommitAndRender();
        CommitAndRender();
        Assert.Equal(1, bitmap.RefCount);

        ElementComposition.SetElementChildVisual(host, visual);
        CommitAndRender();

        var server = (ServerCompositionRecordingVisual)visual.Server;
        Assert.Null(server.ComputeOwnContentBounds());
        Assert.False(visual.HitTest(new Point(10, 10)));
    }
}
