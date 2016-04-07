using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using Perspex;
using Perspex.Animation;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Html;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Data;
using Perspex.Diagnostics;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.Platform;
using Perspex.Threading;
using TestApplication;

namespace TestApplication
{
    class MainWindow
    {
        private static readonly PerspexList<Node> s_treeData = new PerspexList<Node>
        {
            new Node
            {
                Name = "Root 1",
                Children = new PerspexList<Node>
                {
                    new Node
                    {
                        Name = "Child 1",
                    },
                    new Node
                    {
                        Name = "Child 2",
                        Children = new PerspexList<Node>
                        {
                            new Node
                            {
                                Name = "Grandchild 1",
                            },
                            new Node
                            {
                                Name = "Grandmaster Flash",
                            },
                        }
                    },
                    new Node
                    {
                        Name = "Child 3",
                    },
                }
            },
            new Node
            {
                Name = "Root 2",
            },
        };

        private static readonly PerspexList<Item> s_listBoxData = new PerspexList<Item>
        {
            new Item { Name = "Item 1", Value = "Item 1 Value" },
            new Item { Name = "Item 2", Value = "Item 2 Value" },
            new Item { Name = "Item 3", Value = "Item 3 Value" },
            new Item { Name = "Item 4", Value = "Item 4 Value" },
            new Item { Name = "Item 5", Value = "Item 5 Value" },
            new Item { Name = "Item 6", Value = "Item 6 Value" },
            new Item { Name = "Item 7", Value = "Item 7 Value" },
            new Item { Name = "Item 8", Value = "Item 8 Value" },
        };

        public static Window Create()
        {

            TabControl container;


            Window window = new Window
            {
                Title = "Perspex Test Application",
                //Width = 900,
                //Height = 480,
                Content = (container = new TabControl
                {
                    Padding = new Thickness(5),
                    Items = new[]
                    {
                        ButtonsTab(),
                        TextTab(),
                        HtmlTab(),
                        ImagesTab(),
                        ListsTab(),
                        LayoutTab(),
                        AnimationsTab(),
                    },
                    Transition = new CrossFade(TimeSpan.FromSeconds(0.25)),
                    [Grid.RowProperty] = 1,
                    [Grid.ColumnSpanProperty] = 2,
                })

            };

            container.Classes.Add("container");
            
            window.Show();
            return window;
        }

        private static TabItem ButtonsTab()
        {
            var result = new TabItem
            {
                Header = "Button",
                Content = new ScrollViewer()
                {
                    CanScrollHorizontally = false,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(10),
                        Orientation = Orientation.Vertical,
                        Gap = 4,
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "Button",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A button control",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new Button
                            {
                                Width = 150,
                                Content = "Button"
                            },
                            new Button
                            {
                                Width   = 150,
                                Content = "Disabled",
                                IsEnabled = false,
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "ToggleButton",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A toggle button control",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new ToggleButton
                            {
                                Width = 150,
                                IsChecked = true,
                                Content = "On"
                            },
                            new ToggleButton
                            {
                                Width = 150,
                                IsChecked = false,
                                Content = "Off"
                            },
                        }
                    }
                },
            };


            return result;
        }

        private static TabItem HtmlTab()
        {
            return new TabItem
            {
                Header = "Text",
                Content = new ScrollViewer()
                {
                    CanScrollHorizontally = false,
                    Content = new StackPanel()
                    {
                        Margin = new Thickness(10),
                        Orientation = Orientation.Vertical,
                        Gap = 4,
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "TextBlock",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A control for displaying text.",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new TextBlock
                            {
                                Text = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit.",
                                FontSize = 11
                            },
                            new TextBlock
                            {
                                Text = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit.",
                                FontSize = 11,
                                FontWeight = FontWeight.Medium
                            },
                            new TextBlock
                            {
                                Text = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit.",
                                FontSize = 11,
                                FontWeight = FontWeight.Bold
                            },
                            new TextBlock
                            {
                                Text = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit.",
                                FontSize = 11,
                                FontStyle = FontStyle.Italic,
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "HtmlLabel",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A label capable of displaying HTML content",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new HtmlLabel
                            {
                                Background = Brush.Parse("#CCCCCC"),
                                Padding = new Thickness(5),
                                Text = @"<p><strong>Pellentesque habitant morbi tristique</strong> senectus et netus et malesuada fames ac turpis egestas. Vestibulum tortor quam, feugiat vitae, ultricies eget, tempor sit amet, ante. Donec eu libero sit amet quam egestas semper. <em>Aenean ultricies mi vitae est.</em> Mauris placerat eleifend leo. Quisque sit amet est et sapien ullamcorper pharetra. Vestibulum erat wisi, condimentum sed, <code>commodo vitae</code>, ornare sit amet, wisi. Aenean fermentum, elit eget tincidunt condimentum, eros ipsum rutrum orci, sagittis tempus lacus enim ac dui. <a href=""#"">Donec non enim</a> in turpis pulvinar facilisis. Ut felis.</p>
										<h2>Header Level 2</h2>
											       
										<ol>
										   <li>Lorem ipsum dolor sit amet, consectetuer adipiscing elit.</li>
										   <li>Aliquam tincidunt mauris eu risus.</li>
										</ol>

										<blockquote><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. Vivamus pretium ornare est.</p></blockquote>

										<h3>Header Level 3</h3>

										<ul>
										   <li>Lorem ipsum dolor sit amet, consectetuer adipiscing elit.</li>
										   <li>Aliquam tincidunt mauris eu risus.</li>
										</ul>"
                            }
                        }
                    }
                }
            };
        }

        private static TabItem TextTab()
        {
            return new TabItem
            {
                Header = "Input",
                Content = new ScrollViewer()
                {
                    Content = new StackPanel
                    {
                        Margin = new Thickness(10),
                        Orientation = Orientation.Vertical,
                        Gap = 4,
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "TextBox",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A text box control",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },

                            new TextBox { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.", Width = 200},
                            new TextBox { Width = 200, Watermark="Watermark"},
                            new TextBox { Width = 200, Watermark="Floating Watermark", UseFloatingWatermark = true },
                            new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Width = 200, Height = 150, Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. Vivamus pretium ornare est." },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "CheckBox",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A check box control",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new CheckBox { IsChecked = true, Margin = new Thickness(0, 0, 0, 5), Content = "Checked" },
                            new CheckBox { IsChecked = false, Content = "Unchecked" },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "RadioButton",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A radio button control",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new RadioButton { IsChecked = true, Content = "Option 1" },
                            new RadioButton { IsChecked = false, Content = "Option 2" },
                            new RadioButton { IsChecked = false, Content = "Option 3" },
                        }
                    }
                }
            };
        }

        public static string RootNamespace;

        static Stream GetImage(string path)
        {
            return PerspexLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:" + RootNamespace + "." + path));
        }

        private static TabItem ListsTab()
        {
            return new TabItem
            {
                Header = "Lists",
                Content = new ScrollViewer()
                {
                    CanScrollHorizontally = false,
                    Content = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Orientation = Orientation.Vertical,
                        VerticalAlignment = VerticalAlignment.Top,
                        Gap = 4,
                        Margin = new Thickness(10),
                        DataTemplates = new DataTemplates
                        {
                            new FuncDataTemplate<Item>(x =>
                                new StackPanel
                                {
                                    Gap = 4,
                                    Orientation = Orientation.Horizontal,
                                    Children = new Controls
                                    {
                                        new Image { Width = 50, Height = 50, Source = new Bitmap(GetImage("github_icon.png")) },
                                        new TextBlock { Text = x.Name, FontSize = 18 }
                                    }
                                })
                        },
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "ListBox",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A list box control.",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new ListBox
                            {
                                BorderThickness = 2,
                                Items = s_listBoxData,
                                Height = 300,
                                Width =  300,
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "TreeView",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A tree view control.",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new TreeView
                            {
                                Name = "treeView",
                                Items = s_treeData,
                                Height = 300,
                                BorderThickness = 2,
                                Width =  300,
                            }
                        }
                    },
                }
            };
        }

        private static TabItem ImagesTab()
        {
            var imageCarousel = new Carousel
            {
                Width = 400,
                Height = 400,
                Transition = new PageSlide(TimeSpan.FromSeconds(0.25)),
                Items = new[]
                {
                    new Image { Source = new Bitmap(GetImage("github_icon.png")),  Width = 400, Height = 400 },
                    new Image { Source = new Bitmap(GetImage("pattern.jpg")), Width = 400, Height = 400 },
                }
            };

            var next = new Button
            {
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20),
                Content = new Perspex.Controls.Shapes.Path
                {
                    Data = StreamGeometry.Parse("M4,11V13H16L10.5,18.5L11.92,19.92L19.84,12L11.92,4.08L10.5,5.5L16,11H4Z"),
                    Fill = Brushes.Black
                }
            };

            var prev = new Button
            {
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20),
                Content = new Perspex.Controls.Shapes.Path
                {
                    Data = StreamGeometry.Parse("M20,11V13H8L13.5,18.5L12.08,19.92L4.16,12L12.08,4.08L13.5,5.5L8,11H20Z"),
                    Fill = Brushes.Black
                }
            };

            prev.Click += (s, e) =>
            {
                if (imageCarousel.SelectedIndex == 0)
                    imageCarousel.SelectedIndex = 1;
                else
                    imageCarousel.SelectedIndex--;
            };

            next.Click += (s, e) =>
            {
                if (imageCarousel.SelectedIndex == 1)
                    imageCarousel.SelectedIndex = 0;
                else
                    imageCarousel.SelectedIndex++;
            };

            return new TabItem
            {
                Header = "Images",
                Content = new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Orientation = Orientation.Vertical,
                        VerticalAlignment = VerticalAlignment.Top,
                        Gap = 4,
                        Margin = new Thickness(10),
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "Carousel",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "An items control that displays its items as pages that fill the controls.",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new StackPanel
                            {
                                Name = "carouselVisual",
                                Orientation = Orientation.Horizontal,
                                Gap = 4,
                                Children = new Controls
                                {
                                    prev,
                                    imageCarousel,
                                    next
                                }
                            }
                        }
                    }
                }
            };
        }

        private static TabItem LayoutTab()
        {
            var polylinePoints = new Point[] { new Point(0, 0), new Point(5, 0), new Point(6, -2), new Point(7, 3), new Point(8, -3),
                new Point(9, 1), new Point(10, 0), new Point(15, 0) };
            var polygonPoints = new Point[] { new Point(5, 0), new Point(8, 8), new Point(0, 3), new Point(10, 3), new Point(2, 8) };
            for (int i = 0; i < polylinePoints.Length; i++)
            {
                polylinePoints[i] = polylinePoints[i] * 13;
            }
            for (int i = 0; i < polygonPoints.Length; i++)
            {
                polygonPoints[i] = polygonPoints[i] * 15;
            }
            return new TabItem
            {
                Header = "Layout",
                Content = new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Orientation = Orientation.Vertical,
                        VerticalAlignment = VerticalAlignment.Top,
                        Gap = 4,
                        Margin = new Thickness(10),
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "Grid",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "Lays out child controls according to a grid.",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new Grid
                            {
                                Width = 600,
                                ColumnDefinitions = new ColumnDefinitions
                                {
                                    new ColumnDefinition(1, GridUnitType.Star),
                                    new ColumnDefinition(1, GridUnitType.Star),
                                },

                                RowDefinitions = new RowDefinitions
                                {
                                    new RowDefinition(1, GridUnitType.Auto),
                                    new RowDefinition(1, GridUnitType.Auto)
                                },
                                Children = new Controls
                                {

                                    new Rectangle
                                    {
                                        Fill = Brush.Parse("#FF5722"),
                                        [Grid.ColumnSpanProperty] = 2,
                                        Height = 200,
                                        Margin = new Thickness(2.5)
                                    },
                                    new Rectangle
                                    {
                                        Fill = Brush.Parse("#FF5722"),
                                        [Grid.RowProperty] = 1,
                                        Height = 100,
                                        Margin = new Thickness(2.5)
                                    },
                                    new Rectangle
                                    {
                                        Fill = Brush.Parse("#FF5722"),
                                        [Grid.RowProperty] = 1,
                                        [Grid.ColumnProperty] = 1,
                                        Height = 100,
                                        Margin = new Thickness(2.5)
                                    },
                                },
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "StackPanel",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A panel which lays out its children horizontally or vertically.",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                Gap = 4,
                                Width = 300,
                                Children = new Controls
                                {
                                    new Rectangle
                                    {
                                        Fill = Brush.Parse("#FFC107"),
                                        Height = 50,
                                    },
                                    new Rectangle
                                    {
                                        Fill = Brush.Parse("#FFC107"),
                                        Height = 50,
                                    },
                                    new Rectangle
                                    {
                                        Fill = Brush.Parse("#FFC107"),
                                        Height = 50,
                                    },
                                }
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "Canvas",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = Brush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A panel which lays out its children by explicit coordinates.",
                                FontSize = 13,
                                Foreground = Brush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10)
                            },
                            new Canvas
                            {
                                Background = Brushes.Yellow,
                                Width = 300,
                                Height = 400,
                                Children = new Controls
                                {
                                    new Rectangle
                                    {
                                        Fill = Brushes.Blue,
                                        Width = 63,
                                        Height = 41,
                                        [Canvas.LeftProperty] = 40,
                                        [Canvas.TopProperty] = 31,
                                    },
                                    new Ellipse
                                    {
                                        Fill = Brushes.Green,
                                        Width = 58,
                                        Height = 58,
                                        [Canvas.LeftProperty] = 130,
                                        [Canvas.TopProperty] = 79,
                                    },
                                    new Line
                                    {
                                        Stroke = Brushes.Red,
                                        StrokeThickness = 2,
                                        StartPoint = new Point(120, 185),
                                        EndPoint = new Point(30, 115)
                                    },
                                    new Perspex.Controls.Shapes.Path
                                    {
                                        Fill = Brushes.Orange,
                                        Data = StreamGeometry.Parse("M 30,250 c 50,0 50,-50 c 50,0 50,50 h -50 v 50 l -50,-50 Z"),
                                    },
                                    new Polygon
                                    {
                                        Stroke = Brushes.DarkBlue,
                                        Fill = Brushes.Violet,
                                        Points = polygonPoints,
                                        StrokeThickness = 1,
                                        [Canvas.LeftProperty] = 150,
                                        [Canvas.TopProperty] = 180,
                                    },
                                    new Polyline
                                    {
                                        Stroke = Brushes.Brown,
                                        Points = polylinePoints,
                                        StrokeThickness = 5,
                                        StrokeJoin = PenLineJoin.Round,
                                        StrokeStartLineCap = PenLineCap.Triangle,
                                        StrokeEndLineCap = PenLineCap.Triangle,
                                        [Canvas.LeftProperty] = 30,
                                        [Canvas.TopProperty] = 350,
                                    },
                                }
                            },
                        }
                    }
                }
            };
        }

        private static TabItem AnimationsTab()
        {
            Border border1;
            Border border2;
            RotateTransform rotate;
            Button button1;

            var result = new TabItem
            {
                Header = "Animations",
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Gap = 4,
                    Margin = new Thickness(10),
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            Text = "Animations",
                            FontWeight = FontWeight.Medium,
                            FontSize = 20,
                            Foreground = Brush.Parse("#212121"),
                        },
                        new TextBlock
                        {
                            Text = "A few animations showcased below",
                            FontSize = 13,
                            Foreground = Brush.Parse("#727272"),
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        (button1 = new Button
                        {
                            Content = "Animate",
                            Width = 120,
                            [Grid.ColumnProperty] = 1,
                            [Grid.RowProperty] = 1,
                        }),
                        new Canvas
                        {
                            ClipToBounds = false,
                            Children = new Controls
                            {
                                (border1 = new Border
                                {
                                    Width = 100,
                                    Height = 100,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Background = Brushes.Crimson,
                                    RenderTransform = new RotateTransform(),
                                    Child = new Grid
                                    {
                                        Children = new Controls
                                        {
                                            new Ellipse()
                                            {
                                                Width = 100,
                                                Height = 100,
                                                Fill =
                                                    new RadialGradientBrush()
                                                    {
                                                        GradientStops =
                                                        {
                                                            new GradientStop(Colors.Blue, 0),
                                                            new GradientStop(Colors.Green, 1)
                                                        },
                                                        Radius = 75
                                                    }
                                            },
                                            new Perspex.Controls.Shapes.Path
                                            {
                                                Data =
                                                    StreamGeometry.Parse(
                                                        "F1 M 16.6309,18.6563C 17.1309,8.15625 29.8809,14.1563 29.8809,14.1563C 30.8809,11.1563 34.1308,11.4063 34.1308,11.4063C 33.5,12 34.6309,13.1563 34.6309,13.1563C 32.1309,13.1562 31.1309,14.9062 31.1309,14.9062C 41.1309,23.9062 32.6309,27.9063 32.6309,27.9062C 24.6309,24.9063 21.1309,22.1562 16.6309,18.6563 Z M 16.6309,19.9063C 21.6309,24.1563 25.1309,26.1562 31.6309,28.6562C 31.6309,28.6562 26.3809,39.1562 18.3809,36.1563C 18.3809,36.1563 18,38 16.3809,36.9063C 15,36 16.3809,34.9063 16.3809,34.9063C 16.3809,34.9063 10.1309,30.9062 16.6309,19.9063 Z"),
                                                Fill =
                                                    new LinearGradientBrush()
                                                    {
                                                        GradientStops =
                                                        {
                                                            new GradientStop(Colors.Green, 0),
                                                            new GradientStop(Colors.LightSeaGreen, 1)
                                                        }
                                                    },
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                RenderTransform = new MatrixTransform(Matrix.CreateScale(2, 2))
                                            }
                                        }
                                    },
                                    [Canvas.LeftProperty] = 100,
                                    [Canvas.TopProperty] = 100,
                                }),
                                (border2 = new Border
                                {
                                    Width = 100,
                                    Height = 100,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Background = Brushes.Coral,
                                    Child = new Image
                                    {
                                        Source = new Bitmap(GetImage("github_icon.png")),
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                    },
                                    RenderTransform = (rotate = new RotateTransform
                                    {
                                        PropertyTransitions = new PropertyTransitions
                                        {
                                            RotateTransform.AngleProperty.Transition(500),
                                        }
                                    }),
                                    PropertyTransitions = new PropertyTransitions
                                    {
                                        Layoutable.WidthProperty.Transition(300),
                                        Layoutable.HeightProperty.Transition(1000),
                                    },
                                    [Canvas.LeftProperty] = 400,
                                    [Canvas.TopProperty] = 100,
                                }),
                            }
                        }
                    },
                },
            };

            button1.Click += (s, e) =>
            {
                if (border2.Width == 100)
                {
                    border2.Width = border2.Height = 400;
                    rotate.Angle = 180;
                }
                else
                {
                    border2.Width = border2.Height = 100;
                    rotate.Angle = 0;
                }
            };

            var start = Animate.Stopwatch.Elapsed;
            var degrees = Animate.Timer
                .Select(x =>
                {
                    var elapsed = (x - start).TotalSeconds;
                    var cycles = elapsed / 4;
                    var progress = cycles % 1;
                    return 360.0 * progress;
                });

            border1.RenderTransform.Bind(
                RotateTransform.AngleProperty,
                degrees,
                BindingPriority.Animation);

            return result;
        }
    }
}
