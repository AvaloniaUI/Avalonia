using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class DeferredDrawingContextImplTests
    {
        [Fact]
        public void Should_Add_VisualNode()
        {
            var parent = new VisualNode(Mock.Of<IVisual>(), null);
            var child = new VisualNode(Mock.Of<IVisual>(), null);
            var target = new DeferredDrawingContextImpl();

            target.BeginUpdate(parent);
            target.BeginUpdate(child);

            Assert.Equal(1, parent.Children.Count);
            Assert.Same(child, parent.Children[0]);
        }

        [Fact]
        public void Should_Not_Replace_Identical_VisualNode()
        {
            var parent = new VisualNode(Mock.Of<IVisual>(), null);
            var child = new VisualNode(Mock.Of<IVisual>(), null);

            parent.AddChild(child);

            var target = new DeferredDrawingContextImpl();

            target.BeginUpdate(parent);
            target.BeginUpdate(child);

            Assert.Equal(1, parent.Children.Count);
            Assert.Same(child, parent.Children[0]);
        }

        [Fact]
        public void Should_Replace_Different_VisualNode()
        {
            var parent = new VisualNode(Mock.Of<IVisual>(), null);
            var child1 = new VisualNode(Mock.Of<IVisual>(), null);
            var child2 = new VisualNode(Mock.Of<IVisual>(), null);

            parent.AddChild(child1);

            var target = new DeferredDrawingContextImpl();

            target.BeginUpdate(parent);
            target.BeginUpdate(child2);

            Assert.Equal(1, parent.Children.Count);
            Assert.Same(child2, parent.Children[0]);
        }

        [Fact]
        public void TrimChildren_Should_Trim_Children()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);

            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node));
            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node));
            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node));
            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node));

            var target = new DeferredDrawingContextImpl();
            var child1 = new VisualNode(Mock.Of<IVisual>(), null);
            var child2 = new VisualNode(Mock.Of<IVisual>(), null);

            target.BeginUpdate(node);
            using (target.BeginUpdate(child1)) { }
            using (target.BeginUpdate(child2)) { }
            target.TrimChildren();

            Assert.Equal(2, node.Children.Count);
        }

        [Fact]
        public void Should_Add_DrawOperations()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);
            var target = new DeferredDrawingContextImpl();

            node.LayerRoot = node.Visual;

            using (target.BeginUpdate(node))
            {
                target.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100));
                target.DrawRectangle(new Pen(Brushes.Green, 1), new Rect(0, 0, 100, 100));
            }

            Assert.Equal(2, node.DrawOperations.Count);
            Assert.IsType<RectangleNode>(node.DrawOperations[0]);
            Assert.IsType<RectangleNode>(node.DrawOperations[1]);
        }

        [Fact]
        public void Should_Not_Replace_Identical_DrawOperation()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);
            var operation = new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 100, 100), 0);
            var target = new DeferredDrawingContextImpl();

            node.LayerRoot = node.Visual;
            node.AddDrawOperation(operation);

            using (target.BeginUpdate(node))
            {
                target.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100));
            }

            Assert.Equal(1, node.DrawOperations.Count);
            Assert.Same(operation, node.DrawOperations.Single());

            Assert.IsType<RectangleNode>(node.DrawOperations[0]);
        }

        [Fact]
        public void Should_Replace_Different_DrawOperation()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);
            var operation = new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 100, 100), 0);
            var target = new DeferredDrawingContextImpl();

            node.LayerRoot = node.Visual;
            node.AddDrawOperation(operation);

            using (target.BeginUpdate(node))
            {
                target.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100));
            }

            Assert.Equal(1, node.DrawOperations.Count);
            Assert.NotSame(operation, node.DrawOperations.Single());

            Assert.IsType<RectangleNode>(node.DrawOperations[0]);
        }

        [Fact]
        public void Should_Update_DirtyRects()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);
            var operation = new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 100, 100), 0);
            var dirtyRects = new LayerDirtyRects();
            var target = new DeferredDrawingContextImpl(dirtyRects);

            node.LayerRoot = node.Visual;

            using (target.BeginUpdate(node))
            {
                target.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100));
            }

            Assert.Equal(new Rect(0, 0, 100, 100), dirtyRects.Single().Value.Single());
        }

        [Fact]
        public void Should_Trim_DrawOperations()
        {
            var node = new VisualNode(Mock.Of<IVisual>(), null);

            node.LayerRoot = node.Visual;
            node.AddDrawOperation(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 10, 100), 0));
            node.AddDrawOperation(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 20, 100), 0));
            node.AddDrawOperation(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 30, 100), 0));
            node.AddDrawOperation(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 40, 100), 0));

            var target = new DeferredDrawingContextImpl();

            using (target.BeginUpdate(node))
            {
                target.FillRectangle(Brushes.Green, new Rect(0, 0, 10, 100));
                target.FillRectangle(Brushes.Blue, new Rect(0, 0, 20, 100));
            }

            Assert.Equal(2, node.DrawOperations.Count);
        }
    }
}
