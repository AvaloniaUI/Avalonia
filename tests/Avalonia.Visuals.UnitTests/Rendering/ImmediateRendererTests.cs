using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
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
