using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Layout;
using Avalonia.Rendering;

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

                var result = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(result, new LayerDirtyRects());

                Assert.Same(tree, ((VisualNode)result.Root).LayerRoot);
                Assert.Equal(1, result.Root.Children.Count);

                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Same(borderNode, result.FindNode(border));
                Assert.Same(border, borderNode.Visual);
                Assert.Equal(1, borderNode.Children.Count);
                Assert.Equal(1, borderNode.DrawOperations.Count);

                var backgroundNode = (RectangleNode)borderNode.DrawOperations[0];
                Assert.Equal(Brushes.Red, backgroundNode.Brush);

                var textBlockNode = (VisualNode)borderNode.Children[0];
                Assert.Same(textBlockNode, result.FindNode(textBlock));
                Assert.Same(textBlock, textBlockNode.Visual);
                Assert.Equal(1, textBlockNode.DrawOperations.Count);

                var textNode = (TextNode)textBlockNode.DrawOperations[0];
                Assert.NotNull(textNode.Text);
            }
        }

        [Fact]
        public void Should_Respect_Margin_For_ClipBounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Canvas canvas;
                var tree = new TestRoot
                {
                    Width = 200,
                    Height = 300,
                    Child = new Border
                    {
                        Margin = new Thickness(10, 20, 30, 40),
                        Child = canvas = new Canvas
                        {
                            Background = Brushes.AliceBlue,
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

                var result = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(result, new LayerDirtyRects());

                var canvasNode = result.FindNode(canvas);
                Assert.Equal(new Rect(10, 20, 160, 240), canvasNode.ClipBounds);

                // Initial ClipBounds are correct, make sure they're still correct after updating canvas.
                result = result.Clone();
                Assert.True(sceneBuilder.Update(result, canvas, new LayerDirtyRects()));

                canvasNode = result.FindNode(canvas);
                Assert.Equal(new Rect(10, 20, 160, 240), canvasNode.ClipBounds);
            }
        }

        [Fact]
        public void ClipBounds_Should_Be_Intersection_With_Parent_ClipBounds()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border border;
                var tree = new TestRoot
                {
                    Width = 200,
                    Height = 300,
                    Child = new Canvas
                    {
                        ClipToBounds = true,
                        Width = 100,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Children =
                        {
                            (border = new Border
                            {
                                Background = Brushes.AliceBlue,
                                Width = 100,
                                Height = 100,
                                [Canvas.LeftProperty] = 50,
                                [Canvas.TopProperty] = 50,
                            })
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene, new LayerDirtyRects());

                var borderNode = scene.FindNode(border);
                Assert.Equal(new Rect(50, 50, 50, 50), borderNode.ClipBounds);

                // Initial ClipBounds are correct, make sure they're still correct after updating border.
                scene = scene.Clone();
                Assert.True(sceneBuilder.Update(scene, border, new LayerDirtyRects()));

                borderNode = scene.FindNode(border);
                Assert.Equal(new Rect(50, 50, 50, 50), borderNode.ClipBounds);
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

                var result = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(result, new LayerDirtyRects());

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

                var result = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(result, new LayerDirtyRects());

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

                var initial = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(initial, new LayerDirtyRects());

                var initialBackgroundNode = initial.FindNode(border).Children[0];
                var initialTextNode = initial.FindNode(textBlock).DrawOperations[0];

                Assert.NotNull(initialBackgroundNode);
                Assert.NotNull(initialTextNode);

                border.Background = Brushes.Green;

                var result = initial.Clone();
                sceneBuilder.Update(result, border, new LayerDirtyRects());
                
                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Same(border, borderNode.Visual);

                var backgroundNode = (RectangleNode)borderNode.DrawOperations[0];
                Assert.NotSame(initialBackgroundNode, backgroundNode);
                Assert.Equal(Brushes.Green, backgroundNode.Brush);

                var textBlockNode = (VisualNode)borderNode.Children[0];
                Assert.Same(textBlock, textBlockNode.Visual);

                var textNode = (TextNode)textBlockNode.DrawOperations[0];
                Assert.Same(initialTextNode, textNode);
            }
        }

        [Fact]
        public void Should_Update_When_Control_Added()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border border;
                var tree = new TestRoot
                {
                    Width = 100,
                    Height = 100,
                    Child = border = new Border
                    {
                        Background = Brushes.Red,
                    }
                };

                Canvas canvas;
                var decorator = new Decorator
                {
                    Child = canvas = new Canvas(),
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

                var initial = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(initial, new LayerDirtyRects());

                border.Child = decorator;
                var result = initial.Clone();

                Assert.True(sceneBuilder.Update(result, decorator, new LayerDirtyRects()));

                // Updating canvas should result in no-op as it should have been updated along 
                // with decorator as part of the add opeation.
                Assert.False(sceneBuilder.Update(result, canvas, new LayerDirtyRects()));

                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Equal(1, borderNode.Children.Count);
                Assert.Equal(1, borderNode.DrawOperations.Count);

                var decoratorNode = (VisualNode)borderNode.Children[0];
                Assert.Same(decorator, decoratorNode.Visual);
                Assert.Same(decoratorNode, result.FindNode(decorator));

                var canvasNode = (VisualNode)decoratorNode.Children[0];
                Assert.Same(canvas, canvasNode.Visual);
                Assert.Same(canvasNode, result.FindNode(canvas));
            }
        }

        [Fact]
        public void Should_Update_When_Control_Removed()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border border;
                Decorator decorator;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Width = 100,
                    Height = 100,
                    Child = border = new Border
                    {
                        Background = Brushes.Red,
                        Child = decorator = new Decorator
                        {
                            Child = canvas = new Canvas
                            {
                                Background = Brushes.AliceBlue,
                            }
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

                var initial = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(initial, new LayerDirtyRects());

                border.Child = null;
                var result = initial.Clone();

                Assert.True(sceneBuilder.Update(result, decorator, new LayerDirtyRects()));
                Assert.False(sceneBuilder.Update(result, canvas, new LayerDirtyRects()));

                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Equal(0, borderNode.Children.Count);
                Assert.Equal(1, borderNode.DrawOperations.Count);

                Assert.Null(result.FindNode(decorator));
            }
        }

        [Fact]
        public void Should_Update_When_Control_Made_Invisible()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Width = 100,
                    Height = 100,
                    Child = decorator = new Decorator
                    {
                        Child = border = new Border
                        {
                            Background = Brushes.Red,
                            Child = canvas = new Canvas(),
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

                var initial = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(initial, new LayerDirtyRects());

                border.IsVisible = false;
                var result = initial.Clone();

                Assert.True(sceneBuilder.Update(result, border, new LayerDirtyRects()));
                Assert.False(sceneBuilder.Update(result, canvas, new LayerDirtyRects()));

                var decoratorNode = (VisualNode)result.Root.Children[0];
                Assert.Equal(0, decoratorNode.Children.Count);

                Assert.Null(result.FindNode(border));
                Assert.Null(result.FindNode(canvas));
            }
        }

        [Fact]
        public void Should_Update_Descendent_Tranform_When_Margin_Changed()
        {
            using (TestApplication())
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Width = 100,
                    Height = 100,
                    Child = decorator = new Decorator
                    {
                        Margin = new Thickness(0, 10, 0, 0),
                        Child = border = new Border
                        {
                            Child = canvas = new Canvas(),
                        }
                    }
                };

                var layout = AvaloniaLocator.Current.GetService<ILayoutManager>();
                layout.ExecuteInitialLayoutPass(tree);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene, new LayerDirtyRects());

                var borderNode = scene.FindNode(border);
                var canvasNode = scene.FindNode(canvas);
                Assert.Equal(Matrix.CreateTranslation(0, 10), borderNode.Transform);
                Assert.Equal(Matrix.CreateTranslation(0, 10), canvasNode.Transform);

                decorator.Margin = new Thickness(0, 20, 0, 0);
                layout.ExecuteLayoutPass();

                scene = scene.Clone();
                sceneBuilder.Update(scene, decorator, new LayerDirtyRects());

                borderNode = scene.FindNode(border);
                canvasNode = scene.FindNode(canvas);
                Assert.Equal(Matrix.CreateTranslation(0, 20), borderNode.Transform);
                Assert.Equal(Matrix.CreateTranslation(0, 20), canvasNode.Transform);
            }
        }

        [Fact]
        public void DirtyRects_Should_Contain_Old_And_New_Bounds_When_Margin_Changed()
        {
            using (TestApplication())
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Width = 100,
                    Height = 100,
                    Child = decorator = new Decorator
                    {
                        Margin = new Thickness(0, 10, 0, 0),
                        Child = border = new Border
                        {
                            Background = Brushes.Red,
                            Child = canvas = new Canvas(),
                        }
                    }
                };

                var layout = AvaloniaLocator.Current.GetService<ILayoutManager>();
                layout.ExecuteInitialLayoutPass(tree);

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene, new LayerDirtyRects());

                var borderNode = scene.FindNode(border);
                var canvasNode = scene.FindNode(canvas);
                Assert.Equal(Matrix.CreateTranslation(0, 10), borderNode.Transform);
                Assert.Equal(Matrix.CreateTranslation(0, 10), canvasNode.Transform);

                decorator.Margin = new Thickness(0, 20, 0, 0);
                layout.ExecuteLayoutPass();

                scene = scene.Clone();

                var dirty = new LayerDirtyRects();
                sceneBuilder.Update(scene, decorator, dirty);

                var rects = dirty.Single().Value.ToArray();
                Assert.Equal(new[] { new Rect(0, 10, 100, 90) }, rects);
            }
        }

        [Fact]
        public void Control_With_Transparency_Should_Start_New_Layer()
        {
            using (TestApplication())
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Padding = new Thickness(10),
                    Width = 100,
                    Height = 120,
                    Child = decorator = new Decorator
                    {
                        Padding = new Thickness(11),
                        Child = border = new Border
                        {
                            Opacity = 0.5,
                            Background = Brushes.Red,
                            Padding = new Thickness(12),
                            Child = canvas = new Canvas(),
                        }
                    }
                };

                var layout = AvaloniaLocator.Current.GetService<ILayoutManager>();
                layout.ExecuteInitialLayoutPass(tree);

                var dirty = new LayerDirtyRects();
                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene, dirty);

                var rootNode = (VisualNode)scene.Root;
                var borderNode = (VisualNode)scene.FindNode(border);
                var canvasNode = (VisualNode)scene.FindNode(canvas);

                Assert.Same(tree, rootNode.LayerRoot);
                Assert.Same(border, borderNode.LayerRoot);
                Assert.Same(border, canvasNode.LayerRoot);

                Assert.Equal(2, dirty.Count());
                Assert.Empty(dirty.Select(x => x.Key).Except(new IVisual[] { tree, border }));

                border.Opacity = 1;
                scene = scene.Clone();

                dirty = new LayerDirtyRects();
                sceneBuilder.Update(scene, border, dirty);

                rootNode = (VisualNode)scene.Root;
                borderNode = (VisualNode)scene.FindNode(border);
                canvasNode = (VisualNode)scene.FindNode(canvas);

                Assert.Same(tree, rootNode.LayerRoot);
                Assert.Same(tree, borderNode.LayerRoot);
                Assert.Same(tree, canvasNode.LayerRoot);

                Assert.Equal(1, dirty.Count());
                Assert.Equal(tree, dirty.Single().Key);
                Assert.Equal(new Rect(21, 21, 58, 78), dirty.Single().Value.Single());
            }
        }

        private IDisposable TestApplication()
        {
            return UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(
                    layoutManager: new LayoutManager()));
        }
    }
}
