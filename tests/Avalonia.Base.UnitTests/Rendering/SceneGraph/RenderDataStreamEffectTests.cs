using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph;

public class RenderDataStreamEffectTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Effect_Inflates_Child_Bounds_By_Padding(bool unbounded)
    {
        using var stream = new RenderDataStream();
        stream.PushEffect(new ImmutableBlurEffect(5), unbounded ? null : new Rect(0, 0, 100, 100));
        stream.DrawRectangle(null, null, null, new RoundedRect(new Rect(0, 0, 100, 100)), default);
        stream.Pop();

        var padding = ((IEffect)new ImmutableBlurEffect(5)).GetEffectOutputPadding();
        Assert.Equal(new Rect(0, 0, 100, 100).Inflate(padding), stream.CalculateBounds());
    }

    [Fact]
    public void Unbounded_Effect_Replays_Without_A_Clip_Rectangle()
    {
        var effect = new ImmutableBlurEffect(5);
        var context = new Mock<IDrawingContextImplWithEffects>();
        using var stream = new RenderDataStream();
        stream.PushEffect(effect, null);
        stream.DrawRectangle(Brushes.Red, null, null, new RoundedRect(new Rect(0, 0, 100, 100)), default);
        stream.Pop();

        stream.Replay(context.Object);

        context.Verify(x => x.PushEffect(null, effect), Times.Once);
        context.Verify(x => x.PopEffect(), Times.Once);
        Assert.True(stream.HitTest(new Point(50, 50)));
    }

    [Fact]
    public void Empty_Effect_Scope_Has_Null_Bounds()
    {
        using var stream = new RenderDataStream();
        stream.PushEffect(new ImmutableBlurEffect(5), new Rect(0, 0, 100, 100));
        stream.Pop();
        Assert.Null(stream.CalculateBounds());
    }
}
