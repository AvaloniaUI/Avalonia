using System;
using System.Linq;
using Avalonia.Base.UnitTests.VisualTree;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorHitTestingTests : CompositorTestsBase
{

    [Fact]
    public void HitTest_Should_Find_Controls_At_Point()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            var border = new Border
            {
                Width = 100,
                Height = 100,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            s.TopLevel.Content = border;
            
            s.AssertHitTest(new Point(100, 100), null, border);
        }
    }

    [Fact]
    public void HitTest_Should_Not_Find_Empty_Controls_At_Point()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            var border = new Border
            {
                Width = 100,
                Height = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            s.TopLevel.Content = border;

            s.AssertHitTest(new Point(100, 100), null);
        }
    }

    [Fact]
    public void HitTest_Should_Not_Find_Invisible_Controls_At_Point()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border visible, border;
            s.TopLevel.Content = border = new Border
            {
                Width = 100,
                Height = 100,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = false,
                Child = visible = new Border
                {
                    Background = Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            };

            s.AssertHitTest(new Point(100, 100), null);
        }
    }
    
    [Theory,
        InlineData(false, false),
        InlineData(true, false),
        InlineData(false, true),
        InlineData(true, true),
    ]
    public void HitTest_Should_Find_Zero_Opacity_Controls_At_Point(bool parent, bool child)
    {
        
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border visible, border;
            s.TopLevel.Content = border = new Border
            {
                Width = 100,
                Height = 100,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = parent ? 0 : 1,
                Child = visible = new Border
                {
                    Opacity = child ? 0 : 1,
                    Background = Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            };

            s.AssertHitTest(new Point(100, 100), null, visible, border);
        }
    }
    
    [Fact]
    public void HitTest_Should_Not_Find_Control_Outside_Point()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            var border = new Border
            {
                Width = 100,
                Height = 100,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center

            };
            s.TopLevel.Content = border;

            s.AssertHitTest(new Point(10, 10), null);
        }
    }

    [Fact]
    public void HitTest_Should_Return_Top_Controls_First()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Panel container = new Panel
            {
                Width = 200,
                Height = 200,
                Children =
                {
                    new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Border
                    {
                        Width = 50,
                        Height = 50,
                        Background = Brushes.Blue,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
            s.TopLevel.Content = container;

            s.AssertHitTest(new Point(100, 100), null, container.Children[1], container.Children[0]);
        }
    }

    [Fact]
    public void HitTest_Should_Return_Top_Controls_First_With_ZIndex()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Panel container = new Panel
            {
                Width = 200,
                Height = 200,
                Children =
                {
                    new Border
                    {
                        Width = 100,
                        Height = 100,
                        ZIndex = 1,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Border
                    {
                        Width = 50,
                        Height = 50,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new Border
                    {
                        Width = 75,
                        Height = 75,
                        ZIndex = 2,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }

            };
            s.TopLevel.Content = container;

            s.AssertHitTest(new Point(100, 100), null, new[] { container.Children[2], container.Children[0], container.Children[1] });
        }
    }

    [Fact]
    public void HitTest_Should_Find_Control_Translated_Outside_Parent_Bounds()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border target;
            Panel container = new Panel
            {
                Width = 200,
                Height = 200,
                Background = Brushes.Red,
                ClipToBounds = false,
                Children =
                {
                    new Border
                    {
                        Width = 100,
                        Height = 100,
                        ZIndex = 1,
                        Background = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Child = target = new Border
                        {
                            Width = 50,
                            Height = 50,
                            Background = Brushes.Red,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            RenderTransform = new TranslateTransform(110, 110),
                        }
                    },
                }
            };
            s.TopLevel.Content = container;

            s.AssertHitTest(new Point(120, 120), null, target, container);
        }
    }

    [Fact]
    public void HitTest_Should_Not_Find_Control_Outside_Parent_Bounds_When_Clipped()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border target;
            Panel container = new Panel
            {
                Width = 100,
                Height = 200,
                Background = Brushes.Red,
                Children =
                {
                    new Panel()
                    {
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        Margin = new Thickness(0, 100, 0, 0),
                        ClipToBounds = true,
                        Children =
                        {
                            (target = new Border()
                            {
                                Width = 100,
                                Height = 100,
                                Background = Brushes.Red,
                                Margin = new Thickness(0, -100, 0, 0)
                            })
                        }
                    }
                }

            };
            s.TopLevel.Content = container;

            s.AssertHitTest(new Point(50, 50), null, container);
        }
    }

    [Fact]
    public void HitTest_Should_Not_Find_Control_Outside_Scroll_Viewport()
    {
        using (var s = new CompositorTestServices(new Size(100, 200)))
        {
            Border target;
            Border item1;
            Border item2;
            ScrollContentPresenter scroll;
            Panel container = new Panel
            {
                Width = 100,
                Height = 200,
                Background = Brushes.Red,
                Children =
                {
                    (target = new Border()
                    {
                        Name = "b1",
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                    }),
                    new Border()
                    {
                        Name = "b2",
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Red,
                        Margin = new Thickness(0, 100, 0, 0),
                        Child = scroll = new ScrollContentPresenter()
                        {
                            CanHorizontallyScroll = true,
                            CanVerticallyScroll = true,
                            Content = new StackPanel()
                            {
                                Children =
                                {
                                    (item1 = new Border()
                                    {
                                        Name = "b3",
                                        Width = 100,
                                        Height = 100,
                                        Background = Brushes.Red,
                                    }),
                                    (item2 = new Border()
                                    {
                                        Name = "b4",
                                        Width = 100,
                                        Height = 100,
                                        Background = Brushes.Red,
                                    }),
                                }
                            }
                        }
                    }
                }
            };
            s.TopLevel.Content = container;

            scroll.UpdateChild();
            
            s.AssertHitTestFirst(new Point(50, 150), null, item1);

            s.AssertHitTestFirst(new Point(50,50), null, target);
            
            scroll.Offset = new Vector(0, 100);

            s.AssertHitTestFirst(new Point(50, 150), null, item2);
            
            s.AssertHitTestFirst(new Point(50,50), null, target);
        }
    }

    [Fact]
    public void HitTest_Should_Not_Find_Path_When_Outside_Fill()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Path path = new Path
            {
                Width = 200,
                Height = 200,
                Fill = Brushes.Red,
                Data = StreamGeometry.Parse("M100,0 L0,100 100,100")
            };
            s.TopLevel.Content = path;

            s.AssertHitTest(new Point(100, 100), null, path);
            s.AssertHitTest(new Point(10, 10), null);
        }
    }

    [Fact]
    public void HitTest_Should_Respect_Geometry_Clip()
    {
        using (var s = new CompositorTestServices(new Size(400, 400)))
        {
            Canvas canvas;
            Border border = new Border
            {
                Background = Brushes.Red,
                Clip = StreamGeometry.Parse("M100,0 L0,100 100,100"),
                Width = 200,
                Height = 200,
                Child = canvas = new Canvas
                {
                    Background = Brushes.Yellow,
                    Margin = new Thickness(10),
                }

            };
            s.TopLevel.Content = border;

            s.RunJobs();
            Assert.Equal(new Rect(100, 100, 200, 200), border.Bounds);

            s.AssertHitTest(new Point(200,200), null, canvas, border);

            s.AssertHitTest(new Point(110, 110), null);
        }
    }

    [Fact]
    public void HitTest_Should_Accommodate_ICustomHitTest()
    {
        using (var s = new CompositorTestServices(new Size(300, 200)))
        {
            Border border = new CustomHitTestBorder
            {
                ClipToBounds = false,
                Width = 100,
                Height = 100,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            s.TopLevel.Content = border;
            
            s.AssertHitTest(75, 100, null, border);
            s.AssertHitTest(125, 100, null, border);
            s.AssertHitTest(175, 100, null);
        }
    }
    
    [Fact]
    public void HitTest_Should_Not_Hit_Controls_Next_Pixel()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border targetRectangle;

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                Children =
                {
                    new Border { Width = 10, Height = 10, Background= Brushes.Red},
                    { targetRectangle = new Border { Width = 10, Height = 10, Background = Brushes.Green} }
                }
            };

            s.TopLevel.Content = stackPanel;
            
            s.AssertHitTest(new Point(5, 10), null, targetRectangle);
        }
    }
    
    
    [Fact]
    public void HitTest_Filter_Should_Filter_Out_Children()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border child, parent;
            s.TopLevel.Content = parent = new Border
            {
                Width = 100,
                Height = 100,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = child = new Border
                {
                    Background = Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            };

            s.AssertHitTest(new Point(100, 100), null, child, parent);
            s.AssertHitTest(new Point(100, 100), v => v != parent);
        }
    }

    [Fact]
    public void HitTestFirst_Should_Skip_Element_Child_Composition_Visual()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.Red
            };

            s.TopLevel.Content = target;
            s.RunJobs();

            var childVisual = s.Compositor.CreateSolidColorVisual();
            childVisual.Size = new Vector(200, 200);
            childVisual.Color = Colors.Blue;
            ElementComposition.SetElementChildVisual(target, childVisual);

            s.AssertHitTestFirst(new Point(100, 100), null, target);
        }
    }

    [Fact]
    public void HitTest_Should_Find_Control_With_Many_Siblings()
    {
        using (var s = new CompositorTestServices(new Size(1000, 200)))
        {
            Border target = null!;
            var canvas = new Canvas { Width = 1000, Height = 200 };

            for (var i = 0; i < 70; i++)
            {
                var child = new Border { Width = 8, Height = 8, Background = Brushes.Red };
                Canvas.SetLeft(child, i * 12);
                canvas.Children.Add(child);

                if (i == 0)
                    target = child;
            }

            s.TopLevel.Content = canvas;
            s.AssertHitTestFirst(new Point(4, 4), null, target);
        }
    }

    [Fact]
    public void HitTest_Should_Return_Top_Controls_First_With_Many_Overlapping_Siblings()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border top = null!;
            var canvas = new Canvas { Width = 200, Height = 200 };

            for (var i = 0; i < 70; i++)
            {
                var child = new Border { Width = 100, Height = 100, Background = Brushes.Red };
                Canvas.SetLeft(child, 50);
                Canvas.SetTop(child, 50);
                canvas.Children.Add(child);

                if (i == 69)
                    top = child;
            }

            s.TopLevel.Content = canvas;
            s.AssertHitTestFirst(new Point(100, 100), null, top);
            s.AssertHitTest(new Point(100, 100), null, canvas.Children.Cast<Visual>().Reverse().ToArray());
        }
    }

    [Fact]
    public void HitTest_Should_Update_Many_Sibling_Index_When_Child_Moves()
    {
        using (var s = new CompositorTestServices(new Size(1000, 200)))
        {
            Border moving = null!;
            var canvas = new Canvas { Width = 1000, Height = 200 };

            for (var i = 0; i < 70; i++)
            {
                var child = new Border { Width = 8, Height = 8, Background = Brushes.Red };
                Canvas.SetLeft(child, i * 12);
                canvas.Children.Add(child);

                if (i == 69)
                    moving = child;
            }

            s.TopLevel.Content = canvas;
            s.AssertHitTestFirst(new Point(69 * 12 + 4, 4), null, moving);

            Canvas.SetLeft(moving, 10);
            Canvas.SetTop(moving, 100);
            s.AssertHitTestFirst(new Point(14, 104), null, moving);
        }
    }

    [Fact]
    public void HitTest_Should_Update_Many_Sibling_Index_When_Child_Is_Added_And_Removed()
    {
        using (var s = new CompositorTestServices(new Size(200, 200)))
        {
            Border top = null!;
            var canvas = new Canvas { Width = 200, Height = 200 };

            for (var i = 0; i < 70; i++)
            {
                var child = new Border { Width = 100, Height = 100, Background = Brushes.Red };
                Canvas.SetLeft(child, 50);
                Canvas.SetTop(child, 50);
                canvas.Children.Add(child);

                if (i == 69)
                    top = child;
            }

            s.TopLevel.Content = canvas;
            s.AssertHitTestFirst(new Point(100, 100), null, top);

            var added = new Border { Width = 100, Height = 100, Background = Brushes.Blue };
            Canvas.SetLeft(added, 50);
            Canvas.SetTop(added, 50);
            canvas.Children.Add(added);

            s.AssertHitTestFirst(new Point(100, 100), null, added);

            canvas.Children.Remove(added);
            s.AssertHitTestFirst(new Point(100, 100), null, top);
        }
    }

    private static IDisposable TestApplication()
    {
        return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
    }

}
