using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

/// <summary>
/// An immutable recording drawn by a visual is replayed on the render thread from the
/// visual's server render data, which takes its own reference to the recording's stream.
/// </summary>
public class DrawingRecordingLifetimeTests : ScopedTestBase
{
    private readonly CompositorTestServices _services = new();

    public override void Dispose()
    {
        _services.Dispose();
        base.Dispose();
    }

    private sealed class RecordingHost(DrawingRecording recording) : Control
    {
        public override void Render(DrawingContext context) => context.DrawRecording(recording);
    }

    private void CommitAndRender()
    {
        _services.RunJobs();
        _services.Compositor.Commit();
        _services.Compositor.Server.Render(false);
    }

    [Fact]
    public void Server_Keeps_A_Disposed_Recording_Alive_Until_The_Visual_Releases_It()
    {
        var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());
        var rect = new Rect(0, 0, 10, 10);
        var recording = DrawingRecording.Create(ctx => ctx.DrawBitmap(bitmap, 1, rect, rect));

        _services.TopLevel.Content = new RecordingHost(recording) { Width = 100, Height = 100 };
        CommitAndRender();

        // The visual's render data has been sent to the server, which still replays it.
        recording.Dispose();
        CommitAndRender();
        Assert.Equal(2, bitmap.RefCount);

        _services.TopLevel.Content = null;
        CommitAndRender();
        CommitAndRender();
        Assert.Equal(1, bitmap.RefCount);
    }
}
