using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class RenderDataEffectNodeTests
{
    /// <summary>
    /// Regression test: RenderDataEffectNode.Bounds was returning BoundsRect even when
    /// there were no children, causing incorrect dirty rect tracking.
    /// </summary>
    [Fact]
    public void Bounds_Should_Return_Null_When_No_Children()
    {
        var effect = new BlurEffect { Radius = 10 }.ToImmutable();
        var node = new RenderDataEffectNode
        {
            Effect = effect,
            BoundsRect = new Rect(0, 0, 200, 200)
        };

        Assert.Null(node.Bounds);
    }

    /// <summary>
    /// RenderDataEffectNode.Bounds should expand child bounds by the effect output padding
    /// so that dirty rects include effect output (blur/shadow extending beyond geometry bounds).
    /// </summary>
    [Fact]
    public void Bounds_Should_Inflate_Child_Bounds_By_Effect_Output_Padding()
    {
        var effect = new BlurEffect { Radius = 10 }.ToImmutable();
        var childBounds = new Rect(10, 10, 100, 100);
        var node = new RenderDataEffectNode { Effect = effect };
        node.Children.Add(new MockRenderDataItem { MockBounds = childBounds });

        var expectedBounds = childBounds.Inflate(effect.GetEffectOutputPadding());

        Assert.Equal(expectedBounds, node.Bounds);
    }

    private class MockRenderDataItem : IRenderDataItem
    {
        public Rect? MockBounds { get; set; }
        public Rect? Bounds => MockBounds;
        public bool HitTest(Point p) => false;
        public void Invoke(ref RenderDataNodeRenderContext context) { }
    }
}
