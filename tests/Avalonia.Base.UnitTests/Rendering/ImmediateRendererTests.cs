using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering
{
    public class ImmediateRendererTests
    {
        [Fact]
        public void AddDirty_Call_RenderRoot_Invalidate()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var child = new Border
                {
                    Width = 100,
                    Height = 100,
                    Margin = new(10),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                var root = new RenderRoot
                {
                    Child = child,
                    Width = 400,
                    Height = 400,
                };

                root.LayoutManager.ExecuteInitialLayoutPass();

                var target = new ImmediateRenderer(root);

                target.AddDirty(child);

                Assert.Equal(new[] { new Rect(10, 10, 100, 100) }, root.Invalidations);
            }
        }

        [Fact]
        public void AddDirty_With_RenderTransform_Call_RenderRoot_Invalidate()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var child = new Border
                {
                    Width = 100,
                    Height = 100,
                    Margin = new(100),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                var root = new RenderRoot
                {
                    Child = child,
                    Width = 400,
                    Height = 400,
                };

                root.LayoutManager.ExecuteInitialLayoutPass();

                child.RenderTransform = new ScaleTransform() { ScaleX = 2, ScaleY = 2 };

                var target = new ImmediateRenderer(root);

                target.AddDirty(child);

                Assert.Equal(new[] { new Rect(50, 50, 200, 200) }, root.Invalidations);
            }
        }

        [Fact]
        public void AddDirty_For_Child_Moved_Should_Invalidate_Previous_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var child = new Border
                {
                    Width = 100,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                var root = new RenderRoot
                {
                    Child = child,
                    Width = 400,
                    Height = 400,
                };

                var target = new ImmediateRenderer(root);

                root.LayoutManager.ExecuteInitialLayoutPass();
                target.AddDirty(child);

                Assert.Equal(new Rect(0, 0, 100, 100), root.Invalidations[0]);

                target.Paint(new Rect(0, 0, 100, 100));

                //move child 100 pixels bottom/right
                child.Margin = new(100, 100);
                root.LayoutManager.ExecuteLayoutPass();

                //renderer should invalidate old child bounds with new one
                //as on old area there can be artifacts
                target.AddDirty(child);

                //invalidate first old position
                Assert.Equal(new Rect(0, 0, 100, 100), root.Invalidations[1]);

                //then new position
                Assert.Equal(new Rect(100, 100, 100, 100), root.Invalidations[2]);
            }
        }

        [Fact]
        public void Should_Not_Clip_Children_With_RenderTransform_When_In_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                const int RootWidth = 300;
                const int RootHeight = 300;

                var rootGrid = new Grid { Width = RootWidth, Height = RootHeight, ClipToBounds = true };

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0),
                    RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative),
                    RenderTransform = new TransformGroup
                    {
                        Children = { new RotateTransform { Angle = 90 }, new TranslateTransform { X = 240 } }
                    }
                };

                rootGrid.Children.Add(stackPanel);

                TestControl CreateControl()
                    => new TestControl
                    {
                        Width = 80,
                        Height = 40,
                        Margin = new Thickness(0, 0, 5, 0),
                        ClipToBounds = true
                    };

                var control1 = CreateControl();
                var control2 = CreateControl();
                var control3 = CreateControl();

                stackPanel.Children.Add(control1);
                stackPanel.Children.Add(control2);
                stackPanel.Children.Add(control3);

                var root = new TestRoot(rootGrid);
                root.Renderer = new ImmediateRenderer(root);
                root.LayoutManager.ExecuteInitialLayoutPass();

                var rootSize = new Size(RootWidth, RootHeight);
                root.Measure(rootSize);
                root.Arrange(new Rect(rootSize));

                root.Renderer.Paint(root.Bounds);

                Assert.True(control1.Rendered);
                Assert.True(control2.Rendered);
                Assert.True(control3.Rendered);
            }
        }

        [Fact]
        public void Should_Not_Render_Clipped_Child_With_RenderTransform_When_Not_In_Bounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                const int RootWidth = 300;
                const int RootHeight = 300;

                var rootGrid = new Grid { Width = RootWidth, Height = RootHeight, ClipToBounds = true };

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0),
                    RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative),
                    RenderTransform = new TransformGroup
                    {
                        Children = { new RotateTransform { Angle = 90 }, new TranslateTransform { X = 280 } }
                    }
                };

                rootGrid.Children.Add(stackPanel);

                TestControl CreateControl()
                    => new TestControl
                    {
                        Width = 160,
                        Height = 40,
                        Margin = new Thickness(0, 0, 5, 0),
                        ClipToBounds = true
                    };

                var control1 = CreateControl();
                var control2 = CreateControl();
                var control3 = CreateControl();

                stackPanel.Children.Add(control1);
                stackPanel.Children.Add(control2);
                stackPanel.Children.Add(control3);

                var root = new TestRoot(rootGrid);
                root.Renderer = new ImmediateRenderer(root);
                root.LayoutManager.ExecuteInitialLayoutPass();

                var rootSize = new Size(RootWidth, RootHeight);
                root.Measure(rootSize);
                root.Arrange(new Rect(rootSize));

                root.Renderer.Paint(root.Bounds);

                Assert.True(control1.Rendered);
                Assert.True(control2.Rendered);
                Assert.False(control3.Rendered);
            }
        }

        [Fact]
        public void Static_Render_Method_Does_Not_Update_TransformedBounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new Border();
                var expected = new TransformedBounds(new Rect(1, 2, 3, 4), new Rect(4, 5, 6, 7), Matrix.CreateRotation(0.8));

                target.SetTransformedBounds(expected);

                var renderTarget = Mock.Of<IRenderTarget>(x =>
                    x.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>()) == Mock.Of<IDrawingContextImpl>());
                ImmediateRenderer.Render(target, renderTarget);

                Assert.Equal(expected, target.TransformedBounds);
            }
        }

        private class RenderRoot : TestRoot, IRenderRoot
        {
            public List<Rect> Invalidations { get; } = new();
            void IRenderRoot.Invalidate(Rect rect) => Invalidations.Add(rect);
        }

        private class TestControl : Control
        {
            public bool Rendered { get; private set; }

            public override void Render(DrawingContext context)
                => Rendered = true;
        }
    }
}
