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
}
