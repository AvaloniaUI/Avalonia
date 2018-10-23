using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering
{
    public class ImmediateRendererTests
    {
        [Fact]
        public void AddDirty_Call_RenderRoot_Invalidate()
        {
            var visual = new Mock<Visual>();
            var child = new Mock<Visual>();
            var renderRoot = visual.As<IRenderRoot>();

            visual.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(0, 0, 400, 400));

            child.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(10, 10, 100, 100));
            child.As<IVisual>().Setup(v => v.VisualParent).Returns(visual.Object);
            child.As<IVisual>().Setup(v => v.RenderTransform).Returns(default(Transform));
            child.As<IVisual>().Setup(v => v.RenderTransformOrigin).Returns(new RelativePoint(0.5, 0.5, RelativeUnit.Relative));
            child.As<IVisual>().Setup(v => v.TransformToVisual(It.IsAny<IVisual>())).CallBase();

            var target = new ImmediateRenderer(visual.Object);

            target.AddDirty(child.Object);

            renderRoot.Verify(v => v.Invalidate(new Rect(10, 10, 100, 100)));
        }

        [Fact]
        public void AddDirty_With_RenderTransform_Call_RenderRoot_Invalidate()
        {
            var visual = new Mock<Visual>();
            var child = new Mock<Visual>();
            var renderRoot = visual.As<IRenderRoot>();

            visual.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(0, 0, 400, 400));

            child.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(100, 100, 100, 100));
            child.As<IVisual>().Setup(v => v.VisualParent).Returns(visual.Object);
            child.As<IVisual>().Setup(v => v.RenderTransform).Returns(new ScaleTransform() { ScaleX = 2, ScaleY = 2 });
            child.As<IVisual>().Setup(v => v.RenderTransformOrigin).Returns(new RelativePoint(0.5, 0.5, RelativeUnit.Relative));
            child.As<IVisual>().Setup(v => v.TransformToVisual(It.IsAny<IVisual>())).CallBase();

            var target = new ImmediateRenderer(visual.Object);

            target.AddDirty(child.Object);

            renderRoot.Verify(v => v.Invalidate(new Rect(50, 50, 200, 200)));
        }

        [Fact]
        public void AddDirty_For_Child_Moved_Should_Invalidate_Previous_Bounds()
        {
            var visual = new Mock<Visual>();
            var child = new Mock<Visual>();
            var renderRoot = visual.As<IRenderRoot>();
            var renderTarget = visual.As<IRenderTarget>();

            renderRoot.Setup(r => r.CreateRenderTarget()).Returns(renderTarget.Object);
            renderTarget.Setup(r => r.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>())).Returns(Mock.Of<IDrawingContextImpl>());

            visual.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(0, 0, 400, 400));
            visual.As<IVisual>().Setup(v => v.VisualChildren).Returns(new AvaloniaList<IVisual>() { child.As<IVisual>().Object });

            Rect childBounds = new Rect(0, 0, 100, 100);
            child.As<IVisual>().Setup(v => v.Bounds).Returns(() => childBounds);
            child.As<IVisual>().Setup(v => v.VisualParent).Returns(visual.Object);
            child.As<IVisual>().Setup(v => v.TransformToVisual(It.IsAny<IVisual>())).CallBase();
            child.As<IVisual>().Setup(v => v.VisualChildren).Returns(new AvaloniaList<IVisual>());

            var invalidationCalls = new List<Rect>();

            renderRoot.Setup(v => v.Invalidate(It.IsAny<Rect>())).Callback<Rect>(v => invalidationCalls.Add(v));

            var target = new ImmediateRenderer(visual.Object);

            target.AddDirty(child.Object);

            Assert.Equal(new Rect(0, 0, 100, 100), invalidationCalls[0]);

            target.Paint(new Rect(0, 0, 100, 100));

            //move child 100 pixels bottom/right
            childBounds = new Rect(100, 100, 100, 100);

            //renderer should invalidate old child bounds with new one
            //as on old area there can be artifacts
            target.AddDirty(child.Object);

            //invalidate first old position
            Assert.Equal(new Rect(0, 0, 100, 100), invalidationCalls[1]);

            //then new position
            Assert.Equal(new Rect(100, 100, 100, 100), invalidationCalls[2]);
        }

        public class TestVisual : Visual
        {
            public new Rect Bounds
            {
                get => base.Bounds;
                set => base.Bounds = value;
            }
        }
    }
}
