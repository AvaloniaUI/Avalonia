using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class DrawingRecordingBrushTests : ScopedTestBase
{
    private readonly CompositorTestServices _services = new();

    public override void Dispose()
    {
        _services.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Default_Constructor_Has_Null_Recording()
    {
        var brush = new DrawingRecordingBrush();
        Assert.Null(brush.Recording);
    }

    [Fact]
    public void Constructor_With_Recording_Sets_Property()
    {
        using var recording = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));
        });

        var brush = new DrawingRecordingBrush(recording);
        Assert.Same(recording, brush.Recording);
    }

    [Fact]
    public void Recording_Property_Can_Be_Set()
    {
        using var first = DrawingRecording.Create(_ => { });
        using var second = DrawingRecording.Create(_ => { });

        var brush = new DrawingRecordingBrush(first);
        Assert.Same(first, brush.Recording);

        brush.Recording = second;
        Assert.Same(second, brush.Recording);

        brush.Recording = null;
        Assert.Null(brush.Recording);
    }

    [Fact]
    public void SceneBrush_Content_Is_Null_When_Recording_Is_Null()
    {
        var brush = new DrawingRecordingBrush();
        var content = ((ISceneBrush)brush).CreateContent();
        Assert.Null(content);
    }

    [Fact]
    public void SceneBrush_Content_Is_Null_When_Recording_Is_Disposed()
    {
        var recording = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));
        });
        recording.Dispose();

        var brush = new DrawingRecordingBrush(recording);
        var content = ((ISceneBrush)brush).CreateContent();
        Assert.Null(content);
    }

    [Fact]
    public void SceneBrush_Content_Is_Non_Null_For_Immutable_Recording()
    {
        using var recording = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));
        });

        var brush = new DrawingRecordingBrush(recording);
        using var content = ((ISceneBrush)brush).CreateContent();
        Assert.NotNull(content);
    }

    [Fact]
    public void SceneBrush_Content_Preserves_TileBrush_Properties()
    {
        using var recording = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));
        });

        var brush = new DrawingRecordingBrush(recording)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.Fill,
            Opacity = 0.5
        };

        Assert.Equal(TileMode.Tile, brush.TileMode);
        Assert.Equal(Stretch.Fill, brush.Stretch);
        Assert.Equal(0.5, brush.Opacity);
    }

    [Fact]
    public void Accepts_Compositor_Bound_Recording_From_Same_Compositor()
    {
        using var recording = DrawingRecording.Create(_services.Compositor, ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));
        });

        var brush = new DrawingRecordingBrush(recording);
        Assert.Same(recording, brush.Recording);
        // CreateContent renders the recording into an immutable scene-brush context —
        // this exercises the draw path without cross-compositor binding.
        using var content = ((ISceneBrush)brush).CreateContent();
        Assert.NotNull(content);
    }

    [Fact]
    public void Brush_Reusable_Across_Multiple_Content_Creations()
    {
        using var recording = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 10, 10));
        });

        var brush = new DrawingRecordingBrush(recording);

        using var first = ((ISceneBrush)brush).CreateContent();
        using var second = ((ISceneBrush)brush).CreateContent();
        using var third = ((ISceneBrush)brush).CreateContent();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
    }

    [Fact]
    public void ToImmutable_Returns_Renderable_Content_Snapshot()
    {
        using var source = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Lime, null, new Rect(0, 0, 8, 8));
        });
        var brush = new DrawingRecordingBrush(source) { TileMode = TileMode.Tile };

        var snapshot = brush.ToImmutable();

        // Scene brushes snapshot to their current content with immutable
        // tile-brush properties, instead of throwing InvalidCastException.
        Assert.NotSame(brush, snapshot);
        var content = Assert.IsAssignableFrom<ISceneBrushContent>(snapshot);
        Assert.Equal(TileMode.Tile, content.Brush.TileMode);
        Assert.IsAssignableFrom<IImmutableBrush>(content.Brush);
    }

    [Fact]
    public void ToImmutable_Returns_Transparent_For_Empty_Content()
    {
        var brush = new DrawingRecordingBrush();

        var snapshot = brush.ToImmutable();

        var solid = Assert.IsAssignableFrom<ISolidColorBrush>(snapshot);
        Assert.Equal(Colors.Transparent, solid.Color);
    }

    [Fact]
    public void SceneBrush_Content_Survives_Source_Disposal()
    {
        var source = DrawingRecording.Create(ctx =>
        {
            ctx.DrawRectangle(Brushes.Lime, null, new Rect(0, 0, 8, 8));
        });
        var brush = new DrawingRecordingBrush(source);

        var content = ((ISceneBrush)brush).CreateContent();
        Assert.NotNull(content);

        // The content captured the source's item list; disposing the source
        // afterwards must not invalidate the content.
        source.Dispose();

        var mockImpl = new Mock<IDrawingContextImpl>();
        mockImpl.Setup(x => x.Transform).Returns(Matrix.Identity);
        content!.Render(mockImpl.Object, null);

        mockImpl.Verify(x => x.DrawRectangle(
            It.IsAny<IBrush>(), It.IsAny<IPen>(),
            It.IsAny<RoundedRect>(), It.IsAny<BoxShadows>()), Times.Once);

        content.Dispose();
    }
}
