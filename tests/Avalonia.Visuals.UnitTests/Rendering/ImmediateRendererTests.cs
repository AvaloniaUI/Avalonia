using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
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
            var child = new Mock<Visual>() { CallBase = true };
            var renderRoot = visual.As<IRenderRoot>();

            visual.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(0, 0, 400, 400));

            child.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(10, 10, 100, 100));
            child.As<IVisual>().Setup(v => v.VisualParent).Returns(visual.Object);

            var target = new ImmediateRenderer(visual.Object);

            target.AddDirty(child.Object);

            renderRoot.Verify(v => v.Invalidate(new Rect(10, 10, 100, 100)));
        }

        [Fact]
        public void AddDirty_With_RenderTransform_Call_RenderRoot_Invalidate()
        {
            var visual = new Mock<Visual>();
            var child = new Mock<Visual>() { CallBase = true };
            var renderRoot = visual.As<IRenderRoot>();

            visual.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(0, 0, 400, 400));

            child.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(100, 100, 100, 100));
            child.As<IVisual>().Setup(v => v.VisualParent).Returns(visual.Object);
            child.Object.RenderTransform = new ScaleTransform() { ScaleX = 2, ScaleY = 2 };

            var target = new ImmediateRenderer(visual.Object);

            target.AddDirty(child.Object);

            renderRoot.Verify(v => v.Invalidate(new Rect(50, 50, 200, 200)));
        }

        [Fact]
        public void AddDirty_For_Child_Moved_Should_Invalidate_Previous_Bounds()
        {
            var visual = new Mock<Visual>() { CallBase = true };
            var child = new Mock<Visual>() { CallBase = true };
            var renderRoot = visual.As<IRenderRoot>();
            var renderTarget = visual.As<IRenderTarget>();

            renderRoot.Setup(r => r.CreateRenderTarget()).Returns(renderTarget.Object);
            renderTarget.Setup(r => r.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>())).Returns(Mock.Of<IDrawingContextImpl>());

            visual.As<IVisual>().Setup(v => v.Bounds).Returns(new Rect(0, 0, 400, 400));
            visual.As<IVisual>().Setup(v => v.VisualChildren).Returns(new AvaloniaList<IVisual>() { child.As<IVisual>().Object });

            Rect childBounds = new Rect(0, 0, 100, 100);
            child.As<IVisual>().Setup(v => v.Bounds).Returns(() => childBounds);
            child.As<IVisual>().Setup(v => v.VisualParent).Returns(visual.Object);
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

        [Fact]
        public void Should_Render_Child_In_Parent_With_RenderTransform()
        {
            var targetMock = new Mock<Control>() { CallBase = true };
            var target = targetMock.Object;
            target.Width = 100;
            target.Height = 50;
            var child = new Panel()
            {
                RenderTransform = new RotateTransform() { Angle = 90 },
                Children =
                {
                    new Panel()
                    {
                        Children =
                        {
                            target
                        }
                    }
                }
            };

            var visualTarget = targetMock.As<IVisual>();
            int rendered = 0;
            visualTarget.Setup(v => v.Render(It.IsAny<DrawingContext>())).Callback(() => rendered++);

            var root = new TestRoot(child);
            root.Renderer = new ImmediateRenderer(root);

            root.LayoutManager.ExecuteInitialLayoutPass(root);

            root.Measure(new Size(50, 100));
            root.Arrange(new Rect(new Size(50, 100)));

            root.Renderer.Paint(root.Bounds);

            Assert.Equal(1, rendered);
        }

        [Fact]
        public void Should_Render_Child_In_Parent_With_RenderTransform2()
        {
            var targetMock = new Mock<Control>() { CallBase = true };
            var target = targetMock.Object;

            target.Width = 100;
            target.Height = 50;
            target.HorizontalAlignment = HorizontalAlignment.Center;
            target.VerticalAlignment = VerticalAlignment.Center;

            var child = new Panel()
            {
                RenderTransform = new RotateTransform() { Angle = 90 },
                Children =
                {
                    new Panel()
                    {
                        Children =
                        {
                            target
                        }
                    }
                }
            };

            var visualTarget = targetMock.As<IVisual>();
            int rendered = 0;
            visualTarget.Setup(v => v.Render(It.IsAny<DrawingContext>())).Callback(() => rendered++);

            var root = new TestRoot(child);
            root.Renderer = new ImmediateRenderer(root);

            root.LayoutManager.ExecuteInitialLayoutPass(root);

            root.Measure(new Size(300, 100));
            root.Arrange(new Rect(new Size(300, 100)));
            root.Renderer.Paint(root.Bounds);

            Assert.Equal(1, rendered);
        }
    }
}
