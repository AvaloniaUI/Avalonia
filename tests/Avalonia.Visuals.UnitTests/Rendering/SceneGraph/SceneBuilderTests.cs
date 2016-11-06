using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
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
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        Child = textBlock = new TextBlock
                        {
                            Text = "Hello World",
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

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
        public void Should_Respect_ZIndex()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border front;
                Border back;
                var tree = new TestRoot
                {
                    Child = new Panel
                    {
                        Children =
                        {
                            (front = new Border
                            {
                                ZIndex = 1,
                            }),
                            (back = new Border
                            {
                                ZIndex = 0,
                            }),
                        }
                    }
                };

                var result = SceneBuilder.Update(new Scene(tree));

                var panelNode = result.FindNode(tree.Child);
                var expected = new IVisual[] { back, front };
                var actual = panelNode.Children.OfType<IVisualNode>().Select(x => x.Visual).ToArray();
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void ClipBounds_Should_Be_In_Global_Coordinates()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border target;
                var tree = new TestRoot
                {
                    Child = new Decorator
                    {
                        Margin = new Thickness(24, 26),
                        Child = target = new Border
                        {
                            Margin = new Thickness(26, 24),
                            Width = 100,
                            Height = 100,
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

                var result = SceneBuilder.Update(new Scene(tree));
                var targetNode = result.FindNode(target);

                Assert.Equal(new Rect(50, 50, 100, 100), targetNode.ClipBounds);
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
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        Child = textBlock = new TextBlock
                        {
                            Foreground = Brushes.Green,
                            Text = "Hello World",
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

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
                    Width = 100,
                    Height = 100,
                    Child = border = new Border
                    {
                        Background = Brushes.Red,
                        Child = textBlock = new TextBlock
                        {
                            Text = "Hello World",
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

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
