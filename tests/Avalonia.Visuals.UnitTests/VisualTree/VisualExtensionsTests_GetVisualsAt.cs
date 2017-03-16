// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.VisualTree
{
    public class VisualExtensionsTests_GetVisualsAt
    {
        [Fact]
        public void GetVisualsAt_Should_Find_Controls_At_Point()
        {
            using (var application = UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                var container = new Decorator
                {
                    Width = 200,
                    Height = 200,
                    Child = new Border
                    {
                        Width = 100,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(100, 100));

                Assert.Equal(new[] { container.Child, container }, result);
            }
        }

        [Fact]
        public void GetVisualsAt_Should_Not_Find_Invisible_Controls_At_Point()
        {
            using (var application = UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                var container = new Decorator
                {
                    Width = 200,
                    Height = 200,
                    Child = new Border
                    {
                        Width = 100,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsVisible = false,
                        Child = new Border
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                        }
                    }
                };

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(100, 100));

                Assert.Equal(new[] { container }, result);
            }
        }

        [Fact]
        public void GetVisualsAt_Should_Not_Find_Control_Outside_Point()
        {
            using (UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                var container = new Decorator
                {
                    Width = 200,
                    Height = 200,
                    Child = new Border
                    {
                        Width = 100,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(10, 10));

                Assert.Equal(new[] { container }, result);
            }
        }

        [Fact]
        public void GetVisualsAt_Should_Return_Top_Controls_First()
        {
            using (UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                var container = new Panel
                {
                    Width = 200,
                    Height = 200,
                    Children = new Controls.Controls
                    {
                        new Border
                        {
                            Width = 100,
                            Height = 100,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Border
                        {
                            Width = 50,
                            Height = 50,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(100, 100));

                Assert.Equal(new[] { container.Children[1], container.Children[0], container }, result);
            }
        }

        [Fact]
        public void GetVisualsAt_Should_Return_Top_Controls_First_With_ZIndex()
        {
            using (UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                var container = new Panel
                {
                    Width = 200,
                    Height = 200,
                    Children = new Controls.Controls
                    {
                        new Border
                        {
                            Width = 100,
                            Height = 100,
                            ZIndex = 1,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Border
                        {
                            Width = 50,
                            Height = 50,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Border
                        {
                            Width = 75,
                            Height = 75,
                            ZIndex = 2,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(100, 100));

                Assert.Equal(new[] { container.Children[2], container.Children[0], container.Children[1], container }, result);
            }
        }

        [Fact]
        public void GetVisualsAt_Should_Find_Control_Translated_Outside_Parent_Bounds()
        {
            using (UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                Border target;
                var container = new Panel
                {
                    Width = 200,
                    Height = 200,
                    ClipToBounds = false,
                    Children = new Controls.Controls
                    {
                        new Border
                        {
                            Width = 100,
                            Height = 100,
                            ZIndex = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Child = target = new Border
                            {
                                Width = 50,
                                Height = 50,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                RenderTransform = new TranslateTransform(110, 110),
                            }
                        },
                    }
                };

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(120, 120));

                Assert.Equal(new IVisual[] { target, container }, result);
            }
        }

        [Fact]
        public void GetVisualsAt_Should_Not_Find_Control_Outside_Parent_Bounds_When_Clipped()
        {
            using (UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                Border target;

                var container = new Panel
                {
                    Width = 100,
                    Height = 200,
                    Children = new Controls.Controls
                    {
                        new Panel()
                        {
                            Width = 100,
                            Height = 100,
                            Margin = new Thickness(0, 100, 0, 0),
                            ClipToBounds = true,
                            Children = new Controls.Controls
                            {
                                (target = new Border()
                                {
                                    Width = 100,
                                    Height = 100,
                                    Margin = new Thickness(0, -100, 0, 0)
                                })
                            }
                        }
                    }
                };

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(50, 50));

                Assert.Equal(new[] { container }, result);
            }
        }

        [Fact]
        public void GetVisualsAt_Should_Not_Find_Control_Outside_Scroll_Viewport()
        {
            using (UnitTestApplication.Start(new TestServices(renderInterface: new MockRenderInterface())))
            {
                Border target;
                Border item1;
                Border item2;
                ScrollContentPresenter scroll;

                var container = new Panel
                {
                    Width = 100,
                    Height = 200,
                    Children = new Controls.Controls
                    {
                        (target = new Border()
                        {
                            Width = 100,
                            Height = 100
                        }),
                        new Border()
                        {
                            Width = 100,
                            Height = 100,
                            Margin = new Thickness(0, 100, 0, 0),
                            Child = scroll = new ScrollContentPresenter()
                            {
                                Content = new StackPanel()
                                {
                                    Children = new Controls.Controls
                                    {
                                        (item1 = new Border()
                                        {
                                            Width = 100,
                                            Height = 100,
                                        }),
                                        (item2 = new Border()
                                        {
                                            Width = 100,
                                            Height = 100,
                                        }),
                                    }
                                }
                            }
                        }
                    }
                };

                scroll.UpdateChild();

                container.Measure(Size.Infinity);
                container.Arrange(new Rect(container.DesiredSize));

                var context = new DrawingContext(Mock.Of<IDrawingContextImpl>());
                context.Render(container);

                var result = container.GetVisualsAt(new Point(50, 150)).First();

                Assert.Equal(item1, result);

                result = container.GetVisualsAt(new Point(50, 50)).First();

                Assert.Equal(target, result);

                scroll.Offset = new Vector(0, 100);

                //we don't have setup LayoutManager so we will make it manually
                scroll.Parent.InvalidateArrange();
                container.InvalidateArrange();

                container.Arrange(new Rect(container.DesiredSize));
                context.Render(container);

                result = container.GetVisualsAt(new Point(50, 150)).First();

                Assert.Equal(item2, result);

                result = container.GetVisualsAt(new Point(50, 50)).First();

                Assert.NotEqual(item1, result);
                Assert.Equal(target, result);
            }
        }
    }
}
