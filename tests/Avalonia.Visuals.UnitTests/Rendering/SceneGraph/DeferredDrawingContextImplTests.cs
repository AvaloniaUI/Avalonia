using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.Utilities;
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
            var parent = new VisualNode(new TestRoot(), null);
            var child = new VisualNode(Mock.Of<IVisual>(), null);
            var layers = new SceneLayers(parent.Visual);
            var target = new DeferredDrawingContextImpl(null, layers);

            target.BeginUpdate(parent);
            target.BeginUpdate(child);

            Assert.Equal(1, parent.Children.Count);
            Assert.Same(child, parent.Children[0]);
        }

        [Fact]
        public void Should_Not_Replace_Identical_VisualNode()
        {
            var parent = new VisualNode(new TestRoot(), null);
            var child = new VisualNode(Mock.Of<IVisual>(), null);
            var layers = new SceneLayers(parent.Visual);

            parent.AddChild(child);

            var target = new DeferredDrawingContextImpl(null, layers);

            target.BeginUpdate(parent);
            target.BeginUpdate(child);

            Assert.Equal(1, parent.Children.Count);
            Assert.Same(child, parent.Children[0]);
        }

        [Fact]
        public void Should_Replace_Different_VisualNode()
        {
            var parent = new VisualNode(new TestRoot(), null);
            var child1 = new VisualNode(Mock.Of<IVisual>(), null);
            var child2 = new VisualNode(Mock.Of<IVisual>(), null);
            var layers = new SceneLayers(parent.Visual);

            parent.AddChild(child1);

            var target = new DeferredDrawingContextImpl(null, layers);

            target.BeginUpdate(parent);
            target.BeginUpdate(child2);

            Assert.Equal(1, parent.Children.Count);
            Assert.Same(child2, parent.Children[0]);
        }

        [Fact]
        public void TrimChildren_Should_Trim_Children()
        {
            var root = new TestRoot();
            var node = new VisualNode(root, null) { LayerRoot = root };

            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node) { LayerRoot = root });
            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node) { LayerRoot = root });
            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node) { LayerRoot = root });
            node.AddChild(new VisualNode(Mock.Of<IVisual>(), node) { LayerRoot = root });

            var layers = new SceneLayers(root);
            var target = new DeferredDrawingContextImpl(null, layers);
            var child1 = new VisualNode(Mock.Of<IVisual>(), null) { LayerRoot = root };
            var child2 = new VisualNode(Mock.Of<IVisual>(), null) { LayerRoot = root };

            target.BeginUpdate(node);
            using (target.BeginUpdate(child1)) { }
            using (target.BeginUpdate(child2)) { }
            target.TrimChildren();

            Assert.Equal(2, node.Children.Count);
        }

        [Fact]
        public void Should_Add_DrawOperations()
        {
            var node = new VisualNode(new TestRoot(), null);
            var layers = new SceneLayers(node.Visual);
            var target = new DeferredDrawingContextImpl(null, layers);

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
            var node = new VisualNode(new TestRoot(), null);
            var operation = RefCountable.Create(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 100, 100), 0));
            var layers = new SceneLayers(node.Visual);
            var target = new DeferredDrawingContextImpl(null, layers);

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
            var node = new VisualNode(new TestRoot(), null);
            var operation = RefCountable.Create(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 100, 100), 0));
            var layers = new SceneLayers(node.Visual);
            var target = new DeferredDrawingContextImpl(null, layers);

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
            var node = new VisualNode(new TestRoot(), null);
            var operation = new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 100, 100), 0);
            var layers = new SceneLayers(node.Visual);
            var target = new DeferredDrawingContextImpl(null, layers);

            node.LayerRoot = node.Visual;

            using (target.BeginUpdate(node))
            {
                target.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100));
            }

            Assert.Equal(new Rect(0, 0, 100, 100), layers.Single().Dirty.Single());
        }

        [Fact]
        public void Should_Trim_DrawOperations()
        {
            var node = new VisualNode(new TestRoot(), null);

            node.LayerRoot = node.Visual;
            node.AddDrawOperation(RefCountable.Create(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 10, 100), 0)));
            node.AddDrawOperation(RefCountable.Create(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 20, 100), 0)));
            node.AddDrawOperation(RefCountable.Create(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 30, 100), 0)));
            node.AddDrawOperation(RefCountable.Create(new RectangleNode(Matrix.Identity, Brushes.Red, null, new Rect(0, 0, 40, 100), 0)));

            var layers = new SceneLayers(node.Visual);
            var target = new DeferredDrawingContextImpl(null, layers);

            using (target.BeginUpdate(node))
            {
                target.FillRectangle(Brushes.Green, new Rect(0, 0, 10, 100));
                target.FillRectangle(Brushes.Blue, new Rect(0, 0, 20, 100));
            }

            Assert.Equal(2, node.DrawOperations.Count);
        }
    }
}
