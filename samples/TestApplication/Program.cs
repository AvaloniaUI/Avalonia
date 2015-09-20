// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Linq;
using Perspex;
using Perspex.Animation;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Html;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Diagnostics;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
#if PERSPEX_GTK
using Perspex.Gtk;
#endif
using ReactiveUI;

namespace TestApplication
{
    internal class Program
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

        private static void Main(string[] args)
        {
            // The version of ReactiveUI currently included is for WPF and so expects a WPF
            // dispatcher. This makes sure it's initialized.
            System.Windows.Threading.Dispatcher foo = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            new App
            {
                DataTemplates = new DataTemplates
                {
                    new TreeDataTemplate<Node>(
                        x => new TextBlock { Text = x.Name },
                        x => x.Children,
                        x => true),
                },
            };

            TabControl container;

            Window window = new Window
            {
                Title = "Perspex Test Application",
                Width = 900,
                Height = 480,
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star),
                    },
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        (container = new TabControl
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
                    }
                },
            };

            container.Classes.Add(":container");

            window.Show();
            Application.Current.Run(window);
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
								FontSize = 13,
								Foreground = SolidColorBrush.Parse("#212121"),
							},
							new TextBlock
							{
								Text = "A button control",
								FontSize = 20,
								Foreground = SolidColorBrush.Parse("#727272"),
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
								Text = "Toggle button",
								FontWeight = FontWeight.Medium,
								FontSize = 13,
								Foreground = SolidColorBrush.Parse("#212121"),
							},
							new TextBlock
							{
								Text = "A button control",
								FontSize = 20,
								Foreground = SolidColorBrush.Parse("#727272"),
								Margin = new Thickness(0, 0, 0, 10)
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
								Text = "Text block",
								FontWeight = FontWeight.Medium,
								FontSize = 20,
								Foreground = SolidColorBrush.Parse("#212121"),
							},
							new TextBlock
							{
								Text = "A control for displaying text.",
								FontSize = 13,
								Foreground = SolidColorBrush.Parse("#727272"),
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
								Margin = new Thickness(0, 0, 0, 20)
							},
							new TextBlock
							{
								Text = "HTML label",
								FontWeight = FontWeight.Medium,
								FontSize = 20,
								Foreground = SolidColorBrush.Parse("#212121"),
							},
							new TextBlock
							{
								Text = "A label capable of displaying HTML content",
								FontSize = 13,
								Foreground = SolidColorBrush.Parse("#727272"),
								Margin = new Thickness(0, 0, 0, 10)
							},
							new HtmlLabel 
							{ 
								Background = SolidColorBrush.Parse("#CCCCCC"),
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
                Content = new StackPanel
                {
                    Margin = new Thickness(10),
                    Orientation = Orientation.Vertical,
                    Gap = 4,
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            Text = "Check box",
                            FontWeight = FontWeight.Medium,
							FontSize = 20,
							Foreground = SolidColorBrush.Parse("#212121"),
                        },
                        new TextBlock
                        {
                            Text = "A check box control",
                            FontSize = 13,
							Foreground = SolidColorBrush.Parse("#727272"),
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        new CheckBox { IsChecked = true, Margin = new Thickness(0, 0, 0, 5), Content = "Checked" },
                        new CheckBox { IsChecked = false, Content = "Unchecked" },
                        new TextBlock
                        {
                            Margin = new Thickness(0, 40, 0, 0),
                            Text = "Radio button",
                            FontWeight = FontWeight.Medium,
							FontSize = 20,
							Foreground = SolidColorBrush.Parse("#212121"),
                        },
                        new TextBlock
                        {
                            Text = "A radio button control",
							FontSize = 13,
							Foreground = SolidColorBrush.Parse("#727272"),
                            Margin = new Thickness(0, 0, 0, 10)
                        },

                        new RadioButton { IsChecked = true, Margin = new Thickness(0, 0, 0, 5), Content = "Option 1" },
                        new RadioButton { IsChecked = false, Content = "Option 2" },
                        new RadioButton { IsChecked = false, Content = "Option 3" },
                    }
                }
            };
        }

        private static TabItem ImagesTab()
        {
            ScrollBar size;

            return new TabItem
            {
                Header = "Images",
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Gap = 8,
                    Children = new Controls
                    {
						new Perspex.Controls.Shapes.Path {
							Fill = Brushes.Red,
							Stroke = Brushes.Orange,
							Width = 400,
							Height = 400, 
							Data = StreamGeometry.Parse("M18.71,19.5C17.88,20.74 17,21.95 15.66,21.97C14.32,22 13.89,21.18 12.37,21.18C10.84,21.18 10.37,21.95 9.1,22C7.79,22.05 6.8,20.68 5.96,19.47C4.25,17 2.94,12.45 4.7,9.39C5.57,7.87 7.13,6.91 8.82,6.88C10.1,6.86 11.32,7.75 12.11,7.75C12.89,7.75 14.37,6.68 15.92,6.84C16.57,6.87 18.39,7.1 19.56,8.82C19.47,8.88 17.39,10.1 17.41,12.63C17.44,15.65 20.06,16.66 20.09,16.67C20.06,16.74 19.67,18.11 18.71,19.5M13,3.5C13.73,2.67 14.94,2.04 15.94,2C16.07,3.17 15.6,4.35 14.9,5.19C14.21,6.04 13.07,6.7 11.95,6.61C11.8,5.46 12.36,4.26 13,3.5Z")
						},
                        (size = new ScrollBar
                        {
                            Minimum = 100,
                            Maximum = 400,
                            Value = 100,
                            Orientation = Orientation.Horizontal,
                        }),
                        new ScrollViewer
                        {
                            Width = 200,
                            Height = 200,
                            CanScrollHorizontally = true,
                            Content = new Image
                            {
                                Source = new Bitmap("github_icon.png"),
                                [!Layoutable.WidthProperty] = size[!RangeBase.ValueProperty],
                                [!Layoutable.HeightProperty] = size[!RangeBase.ValueProperty],
                            },
                        },
                        new ProgressBar
                        {
                            [!RangeBase.MinimumProperty] = size[!RangeBase.MinimumProperty],
                            [!RangeBase.MaximumProperty] = size[!RangeBase.MaximumProperty],
                            [!RangeBase.ValueProperty] = size[!RangeBase.ValueProperty],
                        }
                    }
                },
            };
        }

        private static TabItem ListsTab()
        {
            ListBox listBox;

            return new TabItem
            {
                Header = "Lists",
                Content = new StackPanel
                {
                    DataTemplates = new DataTemplates
                    {
                        new DataTemplate<Item>(x =>
                            new StackPanel
                            {
                                Children = new Controls
                                {
                                    new TextBlock { Text = x.Name, FontSize = 24 },
                                    new TextBlock { Text = x.Value },
                                }
                            })
                    },
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Gap = 8,
                    Children = new Controls
                    {
                        new TreeView
                        {
                            Name = "treeView",
                            Items = s_treeData,
                        },
                        (listBox = new ListBox
                        {
                            Items = s_listBoxData,
                            MaxHeight = 300,
                        }),
                        new DropDown
                        {
                            Items = s_listBoxData,
                            SelectedItem = s_listBoxData[0],
                            VerticalAlignment = VerticalAlignment.Center,
                        }
                    }
                },
            };
        }

        private static TabItem LayoutTab()
        {
            return new TabItem
            {
                Header = "Layout",
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star),
                    },
                    Margin = new Thickness(50),
                    Children = new Controls
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Gap = 8,
                            Children = new Controls
                            {
                                new Button { HorizontalAlignment = HorizontalAlignment.Left, Content = "Left Aligned" },
                                new Button { HorizontalAlignment = HorizontalAlignment.Center, Content = "Center Aligned" },
                                new Button { HorizontalAlignment = HorizontalAlignment.Right, Content = "Right Aligned" },
                                new Button { HorizontalAlignment = HorizontalAlignment.Stretch, Content = "Stretch" },
                            },
                            [Grid.ColumnProperty] = 0,
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Gap = 8,
                            Children = new Controls
                            {
                                new Button { VerticalAlignment = VerticalAlignment.Top, Content = "Top Aligned" },
                                new Button { VerticalAlignment = VerticalAlignment.Center, Content = "Center Aligned" },
                                new Button { VerticalAlignment = VerticalAlignment.Bottom, Content = "Bottom Aligned" },
                                new Button { VerticalAlignment = VerticalAlignment.Stretch, Content = "Stretch" },
                            },
                            [Grid.ColumnProperty] = 1,
                        },
                    },
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
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star),
                    },
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(GridLength.Auto),
                    },
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
                            Child = new TextBox
                            {
                                Background = Brushes.White,
                                Text = "Hello!",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            },
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
                                Source = new Bitmap("github_icon.png"),
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
                            [Grid.ColumnProperty] = 1,
                        }),
                        (button1 = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Content = "Animate",
                            [Grid.ColumnProperty] = 1,
                            [Grid.RowProperty] = 1,
                        }),
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
