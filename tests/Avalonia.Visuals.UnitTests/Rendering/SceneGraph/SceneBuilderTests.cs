using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class SceneBuilderTests
    {
        [Fact]
        public void Should_Build_Initial_Scene()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border border;
                TextBlock textBlock;
                var tree = new TestRoot
                {
                    Child = border = new Border
                    {
                        Background = Brushes.Red,
                        Child = textBlock = new TextBlock
                        {
                            Text = "Hello World",
                        }
                    }
                };

                var initial = new Scene(tree);
                var result = SceneBuilder.Update(initial);

                Assert.NotSame(initial, result);
                Assert.Equal(1, result.Root.Children.Count);

                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Same(borderNode, result.FindNode(border));
                Assert.Same(border, borderNode.Visual);
                Assert.Equal(2, borderNode.Children.Count);

                var backgroundNode = (RectangleNode)borderNode.Children[0];
                Assert.Equal(Brushes.Red, backgroundNode.Brush);

                var textBlockNode = (VisualNode)borderNode.Children[1];
                Assert.Same(textBlockNode, result.FindNode(textBlock));
                Assert.Same(textBlock, textBlockNode.Visual);
                Assert.Equal(1, textBlockNode.Children.Count);

                var textNode = (TextNode)textBlockNode.Children[0];
                Assert.NotNull(textNode.Text);
            }
        }

        [Fact]
        public void Should_Update_Border_Background_Node()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border border;
                TextBlock textBlock;
                var tree = new TestRoot
                {
                    Child = border = new Border
                    {
                        Background = Brushes.Red,
                        Child = textBlock = new TextBlock
                        {
                            Text = "Hello World",
                        }
                    }
                };

                var initial = SceneBuilder.Update(new Scene(tree));
                var initialBackgroundNode = initial.FindNode(border).Children[0];
                var initialTextNode = initial.FindNode(textBlock).Children[0];

                Assert.NotNull(initialBackgroundNode);
                Assert.NotNull(initialTextNode);

                border.Background = Brushes.Green;
                var result = SceneBuilder.Update(initial);

                Assert.NotSame(initial, result);
                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Same(border, borderNode.Visual);

                var backgroundNode = (RectangleNode)borderNode.Children[0];
                Assert.NotSame(initialBackgroundNode, backgroundNode);
                Assert.Equal(Brushes.Green, backgroundNode.Brush);

                var textBlockNode = (VisualNode)borderNode.Children[1];
                Assert.Same(textBlock, textBlockNode.Visual);

                var textNode = (TextNode)textBlockNode.Children[0];
                Assert.Same(initialTextNode, textNode);
            }
        }

        [Fact]
        public void Should_Update_When_Control_Removed()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border border;
                TextBlock textBlock;
                var tree = new TestRoot
                {
                    Child = border = new Border
                    {
                        Background = Brushes.Red,
                        Child = textBlock = new TextBlock
                        {
                            Text = "Hello World",
                        }
                    }
                };

                var initial = SceneBuilder.Update(new Scene(tree));

                border.Child = null;
                var result = SceneBuilder.Update(initial);

                Assert.NotSame(initial, result);
                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Equal(1, borderNode.Children.Count);
            }
        }
    }
}
