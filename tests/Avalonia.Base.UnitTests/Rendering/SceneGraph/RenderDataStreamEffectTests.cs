using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph;

public class RenderDataStreamEffectTests
{
    [Fact]
    public void Effect_Inflates_Child_Bounds_By_Padding()
    {
        using var stream = new RenderDataStream();
        stream.PushEffect(new ImmutableBlurEffect(5), new Rect(0, 0, 100, 100));
        stream.DrawRectangle(null, null, null, new RoundedRect(new Rect(0, 0, 100, 100)), default);
        stream.Pop();

        var padding = ((IEffect)new ImmutableBlurEffect(5)).GetEffectOutputPadding();
        Assert.Equal(new Rect(0, 0, 100, 100).Inflate(padding), stream.CalculateBounds());
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
