using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class DrawingGroupTests
{
    [Fact]
    public void PushEffect_Should_Store_Provided_Bounds()
    {
        using (UnitTestApplication.Start(new TestServices(renderInterface: Mock.Of<IPlatformRenderInterface>())))
        {
            var group = new DrawingGroup();
            var effect = new BlurEffect { Radius = 10 };
            var bounds = new Rect(10, 10, 100, 100);

            using (var context = group.Open())
            {
                using (context.PushEffect(effect, bounds))
                {
                    context.DrawRectangle(Brushes.Red, null, new Rect(20, 20, 50, 50));
                }
            }

            // The Open() call adds a child DrawingGroup to the root group when PushEffect is called.
            Assert.Single(group.Children);
            var childGroup = Assert.IsType<DrawingGroup>(group.Children[0]);
            Assert.Equal(effect, childGroup.Effect);
            Assert.Equal(bounds, childGroup.EffectBounds);
        }
    }

    [Fact]
    public void Drawing_With_Effect_Should_Use_Stored_Bounds()
    {
        using (UnitTestApplication.Start(new TestServices(renderInterface: Mock.Of<IPlatformRenderInterface>())))
        {
            var effect = new BlurEffect { Radius = 10 };
            var bounds = new Rect(10, 10, 100, 100);
            var group = new DrawingGroup
            {
                Effect = effect,
                EffectBounds = bounds
            };
            group.Children.Add(new GeometryDrawing { Brush = Brushes.Red, Geometry = new RectangleGeometry { Rect = new Rect(20, 20, 50, 50) } });

            var mockContext = new MockDrawingContext();
            group.Draw(mockContext);

            Assert.Equal(effect, mockContext.Effect);
            Assert.Equal(bounds, mockContext.Bounds);
        }
    }

    /// <summary>
    /// Regression test: PlatformDrawingContext.PushEffectCore was passing the raw content bounds to
    /// the platform effect API without inflating by the effect output padding, causing effects to be clipped.
    /// </summary>
    [Fact]
    public void PlatformDrawingContext_PushEffect_Should_Inflate_Bounds_For_Platform_Api()
    {
        var effectImpl = new Mock<IDrawingContextImplWithEffects>();
        effectImpl.Setup(x => x.Transform).Returns(Matrix.Identity);
        using var context = new PlatformDrawingContext(effectImpl.Object, ownsImpl: false);

        var effect = new BlurEffect { Radius = 10 };
        var contentBounds = new Rect(10, 10, 100, 100);
        var expectedInflatedBounds = contentBounds.Inflate(effect.GetEffectOutputPadding());

        using (context.PushEffect(effect, contentBounds)) { }

        // Verify the platform API received the inflated (output) bounds, not the raw content bounds
        effectImpl.Verify(x => x.PushEffect(expectedInflatedBounds, It.IsAny<IEffect>()), Times.Once);
    }

    /// <summary>
    /// Regression test: DrawingGroup.DrawCore was passing uninflated localBounds to PushOpacityMask
    /// when an Effect was also set, meaning the opacity mask didn't cover the effect output region.
    /// </summary>
    [Fact]
    public void DrawCore_With_Effect_And_OpacityMask_Should_Use_EffectBounds_For_OpacityMask()
    {
        using (UnitTestApplication.Start(new TestServices(renderInterface: Mock.Of<IPlatformRenderInterface>())))
        {
            var effect = new BlurEffect { Radius = 10 };
            var contentBounds = new Rect(10, 10, 100, 100);
            var expectedEffectBounds = contentBounds.Inflate(effect.GetEffectOutputPadding());

            var group = new DrawingGroup
            {
                Effect = effect,
                EffectBounds = contentBounds,
                OpacityMask = Brushes.Red
            };
            group.Children.Add(new GeometryDrawing
            {
                Brush = Brushes.Blue,
                Geometry = new RectangleGeometry { Rect = new Rect(20, 20, 50, 50) }
            });

            var mockContext = new MockDrawingContext();
            group.Draw(mockContext);

            Assert.Equal(expectedEffectBounds, mockContext.OpacityMaskBounds);
        }
    }

    private class MockDrawingContext : DrawingContext
    {
        public IEffect? Effect { get; private set; }
        public Rect Bounds { get; private set; }
        public Rect OpacityMaskBounds { get; private set; }

        protected override void PushEffectCore(IEffect effect, Rect bounds)
        {
            Effect = effect;
            Bounds = bounds;
        }

        protected override void PopEffectCore() { }

        // Implementing required abstract members
        protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect) { }
        protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry) { }
        protected override void DrawLineCore(IPen pen, Point p1, Point p2) { }
        protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default) { }
        protected override void PushClipCore(Rect rect) { }
        protected override void PushClipCore(RoundedRect rect) { }
        protected override void PushGeometryClipCore(Geometry clip) { }
        protected override void PushOpacityCore(double opacity) { }
        protected override void PushOpacityMaskCore(IBrush mask, Rect bounds)
        {
            OpacityMaskBounds = bounds;
        }
        protected override void PushTransformCore(Matrix matrix) { }
        protected override void PopClipCore() { }
        protected override void PopGeometryClipCore() { }
        protected override void PopOpacityCore() { }
        protected override void PopOpacityMaskCore() { }
        protected override void PopTransformCore() { }

        protected override void DisposeCore() { }
        internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect) { }
        public override void Custom(ICustomDrawOperation custom) { }
        public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun) { }
        protected override void PushRenderOptionsCore(RenderOptions renderOptions) { }
        protected override void PushTextOptionsCore(TextOptions textOptions) { }
        protected override void PopRenderOptionsCore() { }
        protected override void PopTextOptionsCore() { }
    }
}
