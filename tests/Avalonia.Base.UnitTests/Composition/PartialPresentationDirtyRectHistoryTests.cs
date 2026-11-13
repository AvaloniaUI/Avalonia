using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class PartialPresentationDirtyRectHistoryTests
{
    private static readonly PixelSize s_size = new(200, 200);

    private sealed class Buffer : IPartialPresentationBuffer
    {
        public bool IsValid { get; set; } = true;
        public bool Equals(IPartialPresentationBuffer? other) => ReferenceEquals(this, other);
    }

    private sealed class Presentation : IPartialPresentationFeature
    {
        public IPartialPresentationBuffer? Buffer { get; init; }
        public PixelRect[] FrameDirtyRects { get; private set; } = Array.Empty<PixelRect>();
        public PixelRect[] BufferDirtyRects { get; private set; } = Array.Empty<PixelRect>();

        public void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects, ReadOnlySpan<PixelRect> bufferDirtyRects)
        {
            FrameDirtyRects = frameDirtyRects.ToArray();
            BufferDirtyRects = bufferDirtyRects.ToArray();
        }

        public void Succeeded() { }
    }

    private static SingleDirtyRectTracker Frame(int x, int y)
    {
        var frame = new SingleDirtyRectTracker();
        frame.AddRect(new LtrbRect(x, y, x + 10, y + 10));
        frame.FinalizeFrame(new LtrbRect(0, 0, s_size.Width, s_size.Height));
        return frame;
    }

    private static SingleDirtyRectTracker Prepare(PartialPresentationDirtyRectHistory history,
        IPartialPresentationBuffer? buffer, int x, int y)
    {
        var frame = Frame(x, y);
        history.BeforeDraw(new Presentation { Buffer = buffer }, frame);
        history.BeforeTargetDraw(frame, new LtrbRect(0, 0, s_size.Width, s_size.Height));
        return frame;
    }

    [Fact]
    public void Unfamiliar_And_Invalid_Buffers_Require_A_Full_Repaint()
    {
        var tracker = new PartialPresentationDirtyRectHistory();
        var buffer = new Buffer();
        Prepare(tracker, buffer, 10, 10);
        Assert.Equal(new[] { new PixelRect(s_size) }, tracker.BufferDirtyRects.ToArray());
        tracker.AfterSuccessfulDraw();
        buffer.IsValid = false;
        Prepare(tracker, buffer, 10, 10);
        Assert.Equal(new[] { new PixelRect(s_size) }, tracker.BufferDirtyRects.ToArray());
        Prepare(tracker, null, 10, 10);
        Assert.Equal(new[] { new PixelRect(s_size) }, tracker.BufferDirtyRects.ToArray());
    }

    [Fact]
    public void Repair_Includes_Changes_Since_The_Buffer_Was_Last_Presented()
    {
        var tracker = new PartialPresentationDirtyRectHistory();
        var a = new Buffer();
        var b = new Buffer();
        var c = new Buffer();
        Prepare(tracker, a, 10, 10);
        tracker.AfterSuccessfulDraw();
        Prepare(tracker, b, 50, 50);
        tracker.AfterSuccessfulDraw();
        Prepare(tracker, c, 90, 90);
        tracker.AfterSuccessfulDraw();

        var current = Prepare(tracker, a, 130, 130);
        Assert.Equal(new[] { new PixelRect(129, 129, 12, 12) }, tracker.FrameDirtyRects.ToArray());
        Assert.Equal(new[] { new PixelRect(49, 49, 92, 92) }, tracker.BufferDirtyRects.ToArray());
        // The gaps are repainted too, so culling must include visuals inside them.
        Assert.True(current.Intersects(new LtrbRect(70, 70, 80, 80)));
        var context = new Mock<IDrawingContextImpl>(MockBehavior.Strict);
        context.Setup(x => x.PushClip(new Rect(49, 49, 92, 92)));
        context.Setup(x => x.PopClip());
        using (current.BeginDraw(context.Object)) { }
        context.Verify(x => x.PushClip(new Rect(49, 49, 92, 92)), Times.Once);
        context.Verify(x => x.PopClip(), Times.Once);
        tracker.AfterSuccessfulDraw();

        // Acquisition is not assumed to follow a round-robin order.
        Prepare(tracker, a, 150, 150);
        Assert.Equal(new[] { new PixelRect(149, 149, 12, 12) }, tracker.BufferDirtyRects.ToArray());
    }

    [Fact]
    public void A_Full_Repair_Does_Not_Mark_The_Full_Frame_Dirty_For_Other_Buffers()
    {
        var tracker = new PartialPresentationDirtyRectHistory();
        var a = new Buffer();
        Prepare(tracker, a, 10, 10);
        tracker.AfterSuccessfulDraw();
        Prepare(tracker, new Buffer(), 50, 50);
        Assert.Equal(new[] { new PixelRect(s_size) }, tracker.BufferDirtyRects.ToArray());
        tracker.AfterSuccessfulDraw();
        Prepare(tracker, a, 50, 50);
        Assert.Equal(new[] { new PixelRect(49, 49, 12, 12) }, tracker.BufferDirtyRects.ToArray());
    }

    [Fact]
    public void Historical_Dirty_Rects_Are_Not_Inflated_Again_And_Capturing_Dirty_Rects_Does_Not_Commit()
    {
        var tracker = new PartialPresentationDirtyRectHistory();
        var a = new Buffer();
        Prepare(tracker, a, 10, 10);
        tracker.AfterSuccessfulDraw();
        Prepare(tracker, new Buffer(), 50, 50);
        // Simulate a failed frame by omitting Commit.
        var current = Prepare(tracker, a, 10, 10);
        Assert.Equal(new[] { new PixelRect(9, 9, 12, 12) }, tracker.BufferDirtyRects.ToArray());
        Assert.Equal(new LtrbRect(9, 9, 21, 21), current.CombinedRect);
    }

    [Fact]
    public void Capturing_Frame_Dirty_Rects_Leaves_Scene_Dirty_Rects_Unchanged_Until_Repair_Is_Applied()
    {
        var history = new PartialPresentationDirtyRectHistory();
        var a = new Buffer();
        Prepare(history, a, 10, 10);
        history.AfterSuccessfulDraw();
        Prepare(history, new Buffer(), 50, 50);
        history.AfterSuccessfulDraw();

        var current = Frame(90, 90);
        var presentation = new Presentation { Buffer = a };
        history.BeforeDraw(presentation, current);
        // These are the dirty rectangles used to update the retained layer before repairing the output.
        Assert.Equal(new LtrbRect(89, 89, 101, 101), current.CombinedRect);
        Assert.False(current.Intersects(new LtrbRect(50, 50, 60, 60)));

        history.BeforeTargetDraw(current, new LtrbRect(0, 0, 200, 200));
        Assert.Equal(new LtrbRect(49, 49, 101, 101), current.CombinedRect);
        Assert.Equal(new[] { new PixelRect(89, 89, 12, 12) }, presentation.FrameDirtyRects);
        Assert.Equal(new[] { new PixelRect(49, 49, 52, 52) }, presentation.BufferDirtyRects);
    }

    [Fact]
    public void Rotation_Transforms_Output_Rects_While_History_And_Culling_Stay_In_Scene_Coordinates()
    {
        var history = new PartialPresentationDirtyRectHistory();
        var bounds = new LtrbRect(0, 0, 200, 120);
        var rotation = new Matrix(0, 1, -1, 0, 120, 0);
        var a = new Buffer();
        var b = new Buffer();
        var first = Frame(10, 20);
        history.BeforeDraw(new Presentation { Buffer = a }, first);
        history.BeforeTargetDraw(first, bounds, rotation);
        Assert.Equal(new[] { new PixelRect(0, 0, 120, 200) }, history.BufferDirtyRects.ToArray());
        history.AfterSuccessfulDraw();

        var second = Frame(70, 30);
        history.BeforeDraw(new Presentation { Buffer = b }, second);
        history.BeforeTargetDraw(second, bounds, rotation);
        history.AfterSuccessfulDraw();

        var current = Frame(130, 60);
        var presentation = new Presentation { Buffer = a };
        history.BeforeDraw(presentation, current);
        history.BeforeTargetDraw(current, bounds, rotation);
        Assert.Equal(new LtrbRect(69, 29, 141, 71), current.CombinedRect);
        Assert.Equal(new[] { new PixelRect(49, 129, 12, 12) }, presentation.FrameDirtyRects);
        Assert.Equal(new[] { new PixelRect(49, 69, 42, 72) }, presentation.BufferDirtyRects);
    }

    [Fact]
    public void Buffers_Older_Than_Retained_History_Require_A_Full_Repaint()
    {
        var tracker = new PartialPresentationDirtyRectHistory();
        var a = new Buffer();
        var b = new Buffer();
        Prepare(tracker, a, 10, 10);
        tracker.AfterSuccessfulDraw();
        for (var i = 0; i < 17; i++)
        {
            Prepare(tracker, b, (i % 8) * 20 + 1, (i / 8) * 20 + 1);
            tracker.AfterSuccessfulDraw();
        }
        Prepare(tracker, a, 180, 180);
        Assert.Equal(new[] { new PixelRect(s_size) }, tracker.BufferDirtyRects.ToArray());
    }
}
