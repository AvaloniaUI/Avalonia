using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Layout;
using Moq;
using Avalonia.Platform;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public partial class SceneBuilderTests
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
                sceneBuilder.UpdateAll(result);

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
                sceneBuilder.UpdateAll(result);

                var canvasNode = result.FindNode(canvas);
                Assert.Equal(new Rect(10, 20, 160, 240), canvasNode.ClipBounds);

                // Initial ClipBounds are correct, make sure they're still correct after updating canvas.
                result = result.Clone();
                Assert.True(sceneBuilder.Update(result, canvas));

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
                sceneBuilder.UpdateAll(scene);

                var borderNode = scene.FindNode(border);
                Assert.Equal(new Rect(50, 50, 50, 50), borderNode.ClipBounds);

                // Initial ClipBounds are correct, make sure they're still correct after updating border.
                scene = scene.Clone();
                Assert.True(sceneBuilder.Update(scene, border));

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
                sceneBuilder.UpdateAll(result);

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
                sceneBuilder.UpdateAll(result);

                var targetNode = result.FindNode(target);

                Assert.Equal(new Rect(50, 50, 100, 100), targetNode.ClipBounds);
            }
        }

        [Fact]
        public void Transform_For_Control_With_RenderTransform_Should_Be_Correct_After_Update()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Border border;
                var tree = new TestRoot
                {
                    Width = 400,
                    Height = 200,
                    Child = new Decorator
                    {
                        Width = 200,
                        Height = 100,
                        Child = border = new Border
                        {
                            Background = Brushes.Red,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Width = 100,
                            RenderTransform = new ScaleTransform(0.5, 1),
                        }
                    }
                };

                tree.Measure(Size.Infinity);
                tree.Arrange(new Rect(tree.DesiredSize));

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                var expectedTransform = Matrix.CreateScale(0.5, 1) * Matrix.CreateTranslation(225, 50);
                var borderNode = scene.FindNode(border);
                Assert.Equal(expectedTransform, borderNode.Transform);

                scene = scene.Clone();
                Assert.True(sceneBuilder.Update(scene, border));

                borderNode = scene.FindNode(border);
                Assert.Equal(expectedTransform, borderNode.Transform);
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
                sceneBuilder.UpdateAll(initial);

                var initialBackgroundNode = initial.FindNode(border).Children[0];
                var initialTextNode = initial.FindNode(textBlock).DrawOperations[0];

                Assert.NotNull(initialBackgroundNode);
                Assert.NotNull(initialTextNode);

                border.Background = Brushes.Green;

                var result = initial.Clone();
                sceneBuilder.Update(result, border);
                
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
                sceneBuilder.UpdateAll(initial);

                border.Child = decorator;
                var result = initial.Clone();

                Assert.True(sceneBuilder.Update(result, decorator));

                // Updating canvas should result in no-op as it should have been updated along 
                // with decorator as part of the add opeation.
                Assert.False(sceneBuilder.Update(result, canvas));

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
                sceneBuilder.UpdateAll(initial);

                border.Child = null;
                var result = initial.Clone();

                Assert.True(sceneBuilder.Update(result, decorator));
                Assert.False(sceneBuilder.Update(result, canvas));

                var borderNode = (VisualNode)result.Root.Children[0];
                Assert.Equal(0, borderNode.Children.Count);
                Assert.Equal(1, borderNode.DrawOperations.Count);

                Assert.Null(result.FindNode(decorator));
                Assert.Equal(new Rect(0, 0, 100, 100), result.Layers.Single().Dirty.Single());
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
                sceneBuilder.UpdateAll(initial);

                border.IsVisible = false;
                var result = initial.Clone();

                Assert.True(sceneBuilder.Update(result, border));
                Assert.False(sceneBuilder.Update(result, canvas));

                var decoratorNode = (VisualNode)result.Root.Children[0];
                Assert.Equal(0, decoratorNode.Children.Count);

                Assert.Null(result.FindNode(border));
                Assert.Null(result.FindNode(canvas));
                Assert.Equal(new Rect(0, 0, 100, 100), result.Layers.Single().Dirty.Single());
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
                sceneBuilder.UpdateAll(scene);

                var borderNode = scene.FindNode(border);
                var canvasNode = scene.FindNode(canvas);
                Assert.Equal(Matrix.CreateTranslation(0, 10), borderNode.Transform);
                Assert.Equal(Matrix.CreateTranslation(0, 10), canvasNode.Transform);

                decorator.Margin = new Thickness(0, 20, 0, 0);
                layout.ExecuteLayoutPass();

                scene = scene.Clone();
                sceneBuilder.Update(scene, decorator);

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
                sceneBuilder.UpdateAll(scene);

                var borderNode = scene.FindNode(border);
                var canvasNode = scene.FindNode(canvas);
                Assert.Equal(Matrix.CreateTranslation(0, 10), borderNode.Transform);
                Assert.Equal(Matrix.CreateTranslation(0, 10), canvasNode.Transform);

                decorator.Margin = new Thickness(0, 20, 0, 0);
                layout.ExecuteLayoutPass();

                scene = scene.Clone();

                sceneBuilder.Update(scene, decorator);

                var rects = scene.Layers.Single().Dirty.ToArray();
                Assert.Equal(new[] { new Rect(0, 10, 100, 90) }, rects);
            }
        }

        [Fact]
        public void Resizing_Scene_Should_Add_DirtyRects()
        {
            using (TestApplication())
            {
                Decorator decorator;
                Border border;
                Canvas canvas;
                var tree = new TestRoot
                {
                    Child = decorator = new Decorator
                    {
                        Margin = new Thickness(0, 10, 0, 0),
                        Child = border = new Border
                        {
                            Opacity = 0.5,
                            Background = Brushes.Red,
                            Child = canvas = new Canvas(),
                        }
                    }
                };

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                Assert.Equal(new Size(100, 100), scene.Size);

                tree.ClientSize = new Size(110, 120);
                scene = scene.Clone();
                sceneBuilder.Update(scene, tree);

                Assert.Equal(new Size(110, 120), scene.Size);

                var expected = new[]
                {
                    new Rect(100, 0, 10, 100),
                    new Rect(0, 100, 110, 20),
                };

                Assert.Equal(expected, scene.Layers[tree].Dirty.ToArray());
                Assert.Equal(expected, scene.Layers[border].Dirty.ToArray());
            }
        }

        [Fact]
        public void Should_Set_GeometryClip()
        {
            using (TestApplication())
            {
                var clip = StreamGeometry.Parse("M100,0 L0,100 100,100");
                Decorator decorator;
                var tree = new TestRoot
                {
                    Child = decorator = new Decorator
                    {
                        Clip = clip,
                    }
                };

                var scene = new Scene(tree);
                var sceneBuilder = new SceneBuilder();
                sceneBuilder.UpdateAll(scene);

                var decoratorNode = scene.FindNode(decorator);
                Assert.Same(clip.PlatformImpl, decoratorNode.GeometryClip);
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
