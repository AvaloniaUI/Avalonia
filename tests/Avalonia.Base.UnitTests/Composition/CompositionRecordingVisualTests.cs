using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.UnitTests;
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

    [Fact]
    public void Recording_Disposed_Before_The_Commit_Serializes_As_Empty()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(Brushes.Crimson, null, new Rect(20, 20, 60, 60)));
        var visual = _services.Compositor.CreateRecordingVisual();

        var host = new Control { Width = 100, Height = 100 };
        _services.TopLevel.Content = host;
        _services.RunJobs();
        ElementComposition.SetElementChildVisual(host, visual);

        visual.Recording = recording;
        recording.Dispose();

        _services.RunJobs();
        _services.Compositor.Commit();
        _services.Compositor.Server.Render(false);

        var server = (ServerCompositionRecordingVisual)visual.Server;
        Assert.Null(server.ComputeOwnContentBounds());
    }

    [Fact]
    public void Hit_Test_Treats_A_Recording_Disposed_While_Assigned_As_Empty()
    {
        var recording = DrawingRecording.Create(_services.Compositor, ctx =>
            ctx.DrawRectangle(Brushes.Crimson, null, new Rect(0, 0, 50, 50)));
        var visual = _services.Compositor.CreateRecordingVisual();
        visual.Recording = recording;
        Assert.True(visual.HitTest(new Point(10, 10)));

        // Disposing a recording that is still assigned is the caller's mistake, but hit
        // testing runs during input routing, far from the code that made it.
        recording.Dispose();

        Assert.False(visual.HitTest(new Point(10, 10)));
        Assert.Equal(IntersectionResult.Empty,
            visual.HitTest(new RectangleGeometry(new Rect(0, 0, 20, 20))));
    }
}
