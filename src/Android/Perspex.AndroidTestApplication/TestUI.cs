using Perspex;
using Perspex.Animation;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Html;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Interactivity;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.Platform;
using Perspex.Styling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace TestUI
{
    public class TestUIBuilder
    {
#if __ANDROID__

        //private static string imagePath = "res://component/Perspex Test Application/Perspex.AndroidTestApplication.github_icon.png";
        private static string imagePath = "github_icon.png";

        private static string imagePathP = "pattern.jpg";

        private static double BigFontSize = 75;

        //public static double FontScale = 2;
        //public static double Scale = 2;
        public static double FontScale = 1;

        public static double Scale = 1;
        public static Thickness DefaultMargin = new Thickness(10, 20, 10, 10);
#else
		private static string imagePath = "res://component/TestApplication.github_icon.png";
		private static string imagePathP = "res://component/TestApplication.pattern.jpg";
		//private static string imagePath = "github_icon.png";

		private static double BigFontSize = 80;
		public static double FontScale = 2;
		public static double Scale = 2;
		public static Thickness DefaultMargin = new Thickness(10, 0, 0, 0);
#endif

        private static System.IO.Stream OpenGitHubIconSteam()
        {
            return PerspexLocator.Current.GetService<IAssetLoader>().Open(new Uri(imagePath, imagePath.Contains("://") ? UriKind.Absolute : UriKind.Relative));
        }

        private static System.IO.Stream OpenPatternSteam()
        {
            //"pattern.jpg"
            return PerspexLocator.Current.GetService<IAssetLoader>().Open(new Uri(imagePathP, imagePath.Contains("://") ? UriKind.Absolute : UriKind.Relative));
        }

        internal class Node
        {
            public Node()
            {
                Children = new PerspexList<Node>();
            }

            public string Name { get; set; }
            public PerspexList<Node> Children { get; set; }
        }

        internal class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

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

        public static Window BuildGridWithSomeButtonsAndStuff()
        {
            var buttonToClick = new Button
            {
                Content = "Tap Me",
                Width = 200,
                Foreground = Brushes.Green,
                Margin = new Thickness(5),
                Background = Brushes.Blue,
                FontSize = 50
            };

            var clickCount = 0;
            buttonToClick.Click += (object sender, RoutedEventArgs e) =>
            {
                Console.WriteLine("You clicked it :)");
                buttonToClick.Content = $"Tap x{clickCount}";
                clickCount++;
            };

            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.DarkTurquoise,
                Content = new Grid
                {
                    Margin = DefaultMargin, // skip the status bar area on mobile
                    ColumnDefinitions = new ColumnDefinitions
              {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star)
                    },
                    RowDefinitions = new RowDefinitions
              {
                        new RowDefinition(80, GridUnitType.Pixel),
                        new RowDefinition(160, GridUnitType.Pixel),
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(1, GridUnitType.Star)
                    },
                    Children = new Controls
              {
                        new TextBox
                        {
                            [Grid.RowProperty] = 0,
                            [Grid.ColumnSpanProperty] = 2,
                            Text = "Welcome to Perspex on Android !!!",
                            Foreground = Brushes.SteelBlue,
                            Background = Brushes.White,
                            FontSize = 50,
                            Margin = new Thickness(5, 7, 5, 5),
							//TextAlignment = TextAlignment.Left
						},
////
						new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            [Grid.RowProperty] = 1,
                            [Grid.ColumnSpanProperty] = 2,
                            Background = Brush.Parse("#000000"),
                            Children = new Controls
                            {
                                buttonToClick,
                                new Button
                                {
                                    Content = "Button 2",
                                    Width = 250,
                                    FontSize = 50,
                                    Margin = new Thickness(5)
                                },
                                new Button
                                {
                                    Content = "Button 3",
                                    Width = 250,
                                    FontSize = 50,
                                    Margin = new Thickness(5)
                                }
                            }
                        },
////
						new Image
                        {
                            [Grid.RowProperty] = 2,
                            [Grid.ColumnProperty] = 0,
                            Margin = new Thickness(20),
							//Source = new Bitmap("github_icon.png"),
							Source = new Bitmap(OpenGitHubIconSteam()),
                            Opacity = 0.4
                        },
////
						new Ellipse
                        {
                            [Grid.RowProperty] = 3,
                            [Grid.ColumnProperty] = 0,
                            Margin = new Thickness(20),
                            Fill = Brushes.Blue
                        },
////
						new TextBlock
                        {
                            [Grid.RowProperty] = 2,
                            [Grid.ColumnProperty] = 1,
                            Text =
                                "How does this look? Is this text going to wrap and then look nice within the bounds of this widget? If not I will be extremely disappointed!\n\nWill we start a new paragraph here? If not there will be hell to pay!!!!",
                            Foreground = Brushes.Purple,
                            Background = Brushes.Transparent,
                            FontSize = 28,
                            Margin = new Thickness(10, 30, 10, 10)
                        },
                        new Path
                        {
                            Data =
                                StreamGeometry.Parse(
                                    "M 50,50 l 15,0 l 5,-15 l 5,15 l 15,0 l -10,10 l 4,15 l -15,-9 l -15,9 l 7,-15 Z"),
                            [Grid.RowProperty] = 3,
                            [Grid.ColumnProperty] = 1,
                            Margin = new Thickness(20),
                            Fill = Brushes.White,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 4
                        }
                    }
                }
            };

            return window;
        }

        public static Window BuildSimpleTextUI()
        {
            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = DefaultMargin,
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            Foreground = Brushes.GhostWhite,
                            Background = Brushes.BlueViolet,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Text =
                                "Hello! This is a test of text wrapping and me actually getting something to render for the first time in life. Im and happy to say this was by no means all me. I want to thank everyone who helped me this far.",
                            FontSize = 80,
                            Margin = new Thickness(10),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new CheckBox() {
                            Content = "CheckBox checked",
                            IsChecked = true
                        },
                        new CheckBox() {
                            Content = "CheckBox default",
                            IsChecked = true
                        },
                       new Canvas()
                       {
                           Width = 200,
                           Height = 200,
                           Children = new Controls()
                           {
                               new Path
                                    {
                                        Name = "checkMark",
                                        Fill = new SolidColorBrush(0xff333333),
                                        Width = 200,
                                        Height = 200,
                                        Stretch = Stretch.Uniform,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Data = StreamGeometry.Parse("M 1145.607177734375,430 C1145.607177734375,430 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1138,434.5538330078125 1138,434.5538330078125 1138,434.5538330078125 1141.482177734375,438 1141.482177734375,438 1141.482177734375,438 1141.96875,437.9375 1141.96875,437.9375 1141.96875,437.9375 1147,431.34619140625 1147,431.34619140625 1147,431.34619140625 1145.607177734375,430 1145.607177734375,430 z"),
                                        //Data = StreamGeometry.Parse("M 114.5607177734375,43.0 C114.5607177734375,43.0 114.1449951171875,43.50772705078125 114.1449951171875,43.50772705078125 114.1449951171875,43.50772705078125 113.9232177734375,43.30999755859375 113.9232177734375,43.30999755859375 113.9232177734375,43.30999755859375 1138,43.45538330078125 113.8,43.45538330078125 113.8,43.45538330078125 114.1482177734375,43.8 114.1482177734375,438 114.1482177734375,43.8 114.196875,43.79375 114.196875,43.79375 114.196875,43.79375 114.7,43.134619140625 114.7,43.134619140625 114.7,43.134619140625 114.5607177734375,43.0 114.5607177734375,43.0 z"),
                                        [Grid.ColumnProperty] = 0,
                                    }
                           }
                       }
                    }
                }
            };

            return window;
        }

        public static Window BuildSimpleControlsUI()
        {
            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = DefaultMargin,
                    Children = new Controls
                    {
						//new TextBlock
						//{
						//    Foreground = Brushes.GhostWhite,
						//    Background = Brushes.BlueViolet,
						//    VerticalAlignment = VerticalAlignment.Top,
						//    HorizontalAlignment = HorizontalAlignment.Left,
						//    Text =
						//        "Hello! This is a test of text wrapping and me actually getting something to render for the first time in life. Im and happy to say this was by no means all me. I want to thank everyone who helped me this far.",
						//    FontSize = 80,
						//    Margin = new Thickness(10),
						//    TextWrapping = TextWrapping.Wrap
						//},
						new CheckBox() {
                            Content = "CheckBox checked",
                            IsChecked = true
                        },
						//new CheckBox() {
						//    Content = "CheckBox default",
						//    IsChecked = true
						//},
					}
                }
            };

            return window;
        }

        public static Window BuildSimpleTextBoxUI()
        {
            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = DefaultMargin,
                    Background = Brushes.Yellow,
                    Children = new Controls
                    {
                        new TextBox
                        {
                            Foreground = Brushes.GhostWhite,
                            Background = Brushes.BlueViolet,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Text = "1234567890 aaaaaaaa bbbbbbbbb ccccccccc ddddddddd eeeeeeeeee",
							//Text =
							//    "Hello! This is a test of text wrapping and me actually getting something to render for the first time in life. Im and happy to say this was by no means all me. I want to thank everyone who helped me this far.",
							FontSize = BigFontSize,
                            Margin = new Thickness(10),
                            AcceptsReturn = true,
                            Width=800,
							//AcceptsTab = true,
							TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            };

            return window;
        }

        public static Window BuildStackPanelUI()
        {
            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = DefaultMargin,
                    Children = new Controls
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            [Grid.RowProperty] = 1,
                            [Grid.ColumnSpanProperty] = 2,
                            Background = SolidColorBrush.Parse("#000000"),
                            Children = new Controls
                            {
                                new Button
                                {
                                    Content = "Button 1",
                                    Width = 100,
                                    Foreground = Brushes.Blue,
                                    Margin = new Thickness(5)
                                },
                                new Button
                                {
                                    Content = "Button 2",
                                    Width = 100,
                                    Margin = new Thickness(5)
                                },
                                new Button
                                {
                                    Content = "Button 3",
                                    Width = 100,
                                    Margin = new Thickness(5)
                                }
                            }
                        }
                    }
                }
            };

            return window;
        }

        private static List<string> stringItemsSource;

        public static IEnumerable<string> StringItems
        {
            get
            {
                if (stringItemsSource == null)
                {
                    stringItemsSource = new List<string>();
                    for (int i = 0; i < 10; i++)
                    {
                        stringItemsSource.Add(string.Format($"Item {i}"));
                    }
                }

                return stringItemsSource;
            }
        }

        public static Window BuildListBoxUI()
        {
            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = DefaultMargin,
                    Children = new Controls
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            [Grid.RowProperty] = 0,
                            [Grid.ColumnSpanProperty] = 2,
                            Background = new SolidColorBrush(Colors.Azure),

                            Children = new Controls()
                            {
                                new TextBlock() {Text="vertical listbox" },
                                new ListBox() {Items = StringItems, Margin = new Thickness(10), SelectedItem=StringItems.First() },
                                new TextBlock() {Text="horizontal listbox" },
                                new ListBox()
                                {
                                    Items = StringItems, Margin = new Thickness(10),
                                    [ScrollViewer.HorizontalScrollBarVisibilityProperty] = ScrollBarVisibility.Auto,
                                    VerticalAlignment =VerticalAlignment.Top,
                                    ItemsPanel = new FuncTemplate<IPanel>(()=>new StackPanel() { Orientation=Orientation.Horizontal }) ,
                                    SelectedItem=StringItems.First(),
                                },
                            }
                        }
                    }
                }
            };

            return window;
        }

        public static Window BuildComboBoxUI()
        {
            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = DefaultMargin,
                    Children = new Controls
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            [Grid.RowProperty] = 0,
                            [Grid.ColumnSpanProperty] = 2,
                            Background = new SolidColorBrush(Colors.Azure),

                            Children = new Controls()
                            {
                                new TextBlock() {Text="vertical combobox:" },
                                new DropDown() {Items = StringItems, Margin = new Thickness(10), SelectedItem=StringItems.First() },
                                new TextBlock() {Text="horizontal combobox:" },
                                new DropDown() {
                                    Items = StringItems, Margin = new Thickness(10),
                                    [ScrollViewer.HorizontalScrollBarVisibilityProperty] = ScrollBarVisibility.Auto,
                                    VerticalAlignment =VerticalAlignment.Top,
                                    ItemsPanel = new FuncTemplate<IPanel>(()=>new StackPanel() { Orientation=Orientation.Horizontal, Gap=5 }),
                                    SelectedItem=StringItems.First()
                                },
                            }
                        }
                    }
                }
            };

            Application.Current.DataTemplates.Add(new FuncDataTemplate<string>(s =>
            {
                return new Grid()
                {
                    RowDefinitions = new RowDefinitions()
                    {
                        new RowDefinition(),
                        new RowDefinition()
                    },
                    Children = new Controls()
                    {   new Ellipse() {Fill=Brushes.Orange, [Grid.RowProperty]=0, [Grid.RowSpanProperty]=2, Opacity=0.4  },
						//new Rectangle() { Fill=Brushes.Orange, [Grid.RowProperty]=0  },
						new TextBlock() { Text="Item :", [Grid.RowProperty]=0, Margin=new Thickness(1), FontSize=15, Foreground=Brushes.Blue },
                        new TextBlock() { Text=s, [Grid.RowProperty]=1, Margin=new Thickness(5), FontSize=15 },
                    }
                };
                //return new TextBlock() { Text = s, Margin = new Thickness(5) };
            }));

            return window;
        }

        public static Control DropDownControlTemplate(DropDown control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TemplatedControl.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TemplatedControl.BorderThicknessProperty],
                Child = new Grid
                {
                    Name = "container",
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        new ContentControl
                        {
                            Name = "contentControl",
                            Margin = new Thickness(3),
                            [~ContentControl.ContentProperty] = control[~DropDown.SelectionBoxItemProperty],
                            [~Layoutable.HorizontalAlignmentProperty] = control[~DropDown.HorizontalContentAlignmentProperty],
                            [~Layoutable.VerticalAlignmentProperty] = control[~DropDown.VerticalContentAlignmentProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                            BorderThickness = 0,
                            Background = Brushes.Transparent,
                            ClickMode = ClickMode.Press,
                            Focusable = false,
                            Content = new Path
                            {
                                Name = "checkMark",
                                Fill = Brushes.Black,
                                Width = 8,
                                Height = 4,
                                Stretch = Stretch.Uniform,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Data = StreamGeometry.Parse("F1 M 301.14,-189.041L 311.57,-189.041L 306.355,-182.942L 301.14,-189.041 Z"),
                                [Grid.ColumnProperty] = 0,
                            },
                            [~~ToggleButton.IsCheckedProperty] = control[~~DropDown.IsDropDownOpenProperty],
                            [Grid.ColumnProperty] = 1,
                        },
                        new Popup
                        {
                            Name = "popup",
                            Child = new Border
                            {
                                BorderBrush = Brushes.Black,
                                BorderThickness = 1,
                                Padding = new Thickness(4),
                                Child = new ItemsPresenter
                                {
                                    MemberSelector = control.MemberSelector,
                                    [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                                }
                            },
                            PlacementTarget = control,
                            StaysOpen = false,
                            [~~Popup.IsOpenProperty] = control[~~DropDown.IsDropDownOpenProperty],
                            [~Layoutable.MinWidthProperty] = control[~Visual.BoundsProperty].Cast<Rect>().Select(x => (object)x.Width),
                        }
                    },
                },
            };

            return result;
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
                        Margin = new Thickness(10 * Scale),
                        Orientation = Orientation.Vertical,
                        Gap = 4 * Scale,
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "Button",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A button control",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new Button
                            {
                                Width = 150*Scale,
                                Content = "Button"
                            },
                            new Button
                            {
                                Width   = 150*Scale,
                                Content = "Disabled",
                                IsEnabled = false,
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40*Scale, 0, 0),
                                Text = "ToggleButton",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A toggle button control",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new ToggleButton
                            {
                                Width = 150*Scale,
                                IsChecked = true,
                                Content = "On"
                            },
                            new ToggleButton
                            {
                                Width = 150*Scale,
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
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "HtmlLabel",
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
                Content = new ScrollViewer()
                {
                    Content = new StackPanel
                    {
                        Margin = new Thickness(10 * Scale),
                        Orientation = Orientation.Vertical,
                        Gap = 4 * Scale,
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "TextBox",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A text box control",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },

                            new TextBox { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.", Width = 200},
                            new TextBox { Width = 200*Scale, Watermark="Watermark"},
                            new TextBox { Width = 200*Scale, Watermark="Floating Watermark", UseFloatingWatermark = true },
                            new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Width = 200*Scale, Height = 150*Scale, Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. Vivamus pretium ornare est." },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40*Scale, 0, 0),
                                Text = "CheckBox",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A check box control",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new CheckBox { IsChecked = true, Margin = new Thickness(0, 0, 0, 5*Scale), Content = "Checked" },
                            new CheckBox { IsChecked = false, Content = "Unchecked" },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40*Scale, 0, 0),
                                Text = "RadioButton",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A radio button control",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new RadioButton { IsChecked = true, Content = "Option 1" },
                            new RadioButton { IsChecked = false, Content = "Option 2" },
                            new RadioButton { IsChecked = false, Content = "Option 3" },
                        }
                    }
                }
            };
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
                        Gap = 4 * Scale,
                        Margin = new Thickness(10),
                        DataTemplates = new DataTemplates
                        {
                            new FuncDataTemplate<Item>(x =>
                                new StackPanel
                                {
                                    Gap = 4*Scale,
                                    Orientation = Orientation.Horizontal,
                                    Children = new Controls
                                    {
                                        new Image { Width = 50*Scale, Height = 50*Scale, Source = new Bitmap(OpenGitHubIconSteam()) },
                                        new TextBlock { Text = x.Name, FontSize = 18*FontScale }
                                    }
                                })
                        },
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "ListBox",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A list box control.",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new ListBox
                            {
                                BorderThickness = 2*Scale,
                                Items = s_listBoxData,
                                Height = 300*Scale,
                                Width =  300*Scale,
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40*Scale, 0, 0),
                                Text = "TreeView",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A tree view control.",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new TreeView
                            {
                                Name = "treeView",
                                Items = s_treeData,
                                Height = 300*Scale,
                                BorderThickness = 2*Scale,
                                Width =  300*Scale,
                            }
                        }
                    },
                }
            };
        }

        private static TabItem ImagesTab()
        {
            var imageDeck = new Carousel
            {
                Width = 400 * Scale,
                Height = 400 * Scale,
                Transition = new PageSlide(TimeSpan.FromSeconds(0.25)),
                Items = new[]
                {
                    new Image { Source = new Bitmap(OpenGitHubIconSteam()),  Width = 400*Scale, Height = 400*Scale },
                    new Image { Source = new Bitmap(OpenPatternSteam()), Width = 400*Scale, Height = 400*Scale },
                }
            };

            // imageDeck.AutoSelect = true;

            var next = new Button
            {
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20 * Scale),
                Content = new Perspex.Controls.Shapes.Path
                {
                    Data = StreamGeometry.Parse("M4,11V13H16L10.5,18.5L11.92,19.92L19.84,12L11.92,4.08L10.5,5.5L16,11H4Z"),
                    Fill = Brushes.Black
                }
            };

            var prev = new Button
            {
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20 * Scale),
                Content = new Perspex.Controls.Shapes.Path
                {
                    Data = StreamGeometry.Parse("M20,11V13H8L13.5,18.5L12.08,19.92L4.16,12L12.08,4.08L13.5,5.5L8,11H20Z"),
                    Fill = Brushes.Black
                }
            };

            prev.Click += (s, e) =>
            {
                if (imageDeck.SelectedIndex == 0)
                    imageDeck.SelectedIndex = 1;
                else
                    imageDeck.SelectedIndex--;
            };

            next.Click += (s, e) =>
            {
                if (imageDeck.SelectedIndex == 1)
                    imageDeck.SelectedIndex = 0;
                else
                    imageDeck.SelectedIndex++;
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
                        Gap = 4 * Scale,
                        Margin = new Thickness(10 * Scale),
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "Deck",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "An items control that displays its items as pages that fill the controls.",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new StackPanel
                            {
                                Name = "deckVisual",
                                Orientation = Orientation.Horizontal,
                                Gap = 4*Scale,
                                Children = new Controls
                                {
                                    prev,
                                    imageDeck,
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
                        Gap = 4 * Scale,
                        Margin = new Thickness(10 * Scale),
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "Grid",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "Lays out child controls according to a grid.",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new Grid
                            {
                                Width = 600*Scale,
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
                                        Fill = SolidColorBrush.Parse("#FF5722"),
                                        [Grid.ColumnSpanProperty] = 2,
                                        Height = 200*Scale,
                                        Margin = new Thickness(2.5*Scale)
                                    },
                                    new Rectangle
                                    {
                                        Fill = SolidColorBrush.Parse("#FF5722"),
                                        [Grid.RowProperty] = 1,
                                        Height = 100*Scale,
                                        Margin = new Thickness(2.5*Scale)
                                    },
                                    new Rectangle
                                    {
                                        Fill = SolidColorBrush.Parse("#FF5722"),
                                        [Grid.RowProperty] = 1,
                                        [Grid.ColumnProperty] = 1,
                                        Height = 100*Scale,
                                        Margin = new Thickness(2.5*Scale)
                                    },
                                },
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40*Scale, 0, 0),
                                Text = "StackPanel",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*Scale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A panel which lays out its children horizontally or vertically.",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                Gap = 4*Scale,
                                Width = 300*Scale,
                                Children = new Controls
                                {
                                    new Rectangle
                                    {
                                        Fill = SolidColorBrush.Parse("#FFC107"),
                                        Height = 50*Scale,
                                    },
                                    new Rectangle
                                    {
                                        Fill = SolidColorBrush.Parse("#FFC107"),
                                        Height = 50*Scale,
                                    },
                                    new Rectangle
                                    {
                                        Fill = SolidColorBrush.Parse("#FFC107"),
                                        Height = 50*Scale,
                                    },
                                }
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(0, 40*Scale, 0, 0),
                                Text = "Canvas",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20*FontScale,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A panel which lays out its children by explicit coordinates.",
                                FontSize = 13*FontScale,
                                Foreground = SolidColorBrush.Parse("#727272"),
                                Margin = new Thickness(0, 0, 0, 10*Scale)
                            },
                            new Canvas
                            {
                                Background = Brushes.Yellow,
                                Width = 300*Scale,
                                Height = 400*Scale,
                                Children = new Controls
                                {
                                    new Rectangle
                                    {
                                        Fill = Brushes.Blue,
                                        Width = 63*Scale,
                                        Height = 41*Scale,
                                        [Canvas.LeftProperty] = 40*Scale,
                                        [Canvas.TopProperty] = 31*Scale,
                                    },
                                    new Ellipse
                                    {
                                        Fill = Brushes.Green,
                                        Width = 58*Scale,
                                        Height = 58*Scale,
                                        [Canvas.LeftProperty] = 130*Scale,
                                        [Canvas.TopProperty] = 79*Scale,
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
                    Gap = 4 * Scale,
                    Margin = new Thickness(10 * Scale),
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            Text = "Animations",
                            FontWeight = FontWeight.Medium,
                            FontSize = 20*FontScale,
                            Foreground = SolidColorBrush.Parse("#212121"),
                        },
                        new TextBlock
                        {
                            Text = "A few animations showcased below",
                            FontSize = 13*FontScale,
                            Foreground = SolidColorBrush.Parse("#727272"),
                            Margin = new Thickness(0, 0, 0, 10*Scale)
                        },
                        (button1 = new Button
                        {
                            Content = "Animate",
                            Width = 120*Scale,
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
                                    Width = 100*Scale,
                                    Height = 100*Scale,
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
                                    [Canvas.LeftProperty] = 100*Scale,
                                    [Canvas.TopProperty] = 100*Scale,
                                }),
                                (border2 = new Border
                                {
                                    Width = 100*Scale,
                                    Height = 100*Scale,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Background = Brushes.Coral,
                                    Child = new Image
                                    {
                                        Source = new Bitmap(OpenGitHubIconSteam()),
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
                                    [Canvas.LeftProperty] = 400*Scale,
                                    [Canvas.TopProperty] = 100*Scale,
                                }),
                            }
                        }
                    },
                },
            };
            //bool started = false;
            button1.Click += (s, e) =>
            {
                if (border2.Width == 100 * Scale)
                {
                    border2.Width = border2.Height = 400 * Scale;
                    rotate.Angle = 180;
                }
                else
                {
                    border2.Width = border2.Height = 100 * Scale;
                    rotate.Angle = 0;
                }

                //IDisposable db = null;
                //IObservable<double> degrees = null;
                //if (started == false)
                //{
                //	//Animate.Stopwatch.Restart();
                //	var start = Animate.Stopwatch.Elapsed;
                //	if (degrees == null)
                //	{
                //		degrees = Animate.Timer
                //			.Select(x =>
                //			{
                //				var elapsed = (x - start).TotalSeconds;
                //				var cycles = elapsed / 4;
                //				var progress = cycles % 1;
                //				return 360.0 * progress;
                //			});
                //	}
                //	db = border1.RenderTransform.Bind(
                //		RotateTransform.AngleProperty,
                //		degrees,
                //		BindingPriority.Animation);
                //	started = true;
                //}
                //else
                //{
                //	if (db != null)
                //	{
                //		db.Dispose();
                //		db = null;
                //	}

                //	border1.RenderTransform.ClearValue(RotateTransform.AngleProperty);
                //	border1.RenderTransform = new RotateTransform() { Angle = 0.0 };
                //	started = false;
                //}
            };

            //#if !__ANDROID__
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
            //#endif
            return result;
        }

        private static Window CreateComplexTabUI()
        {
            // The version of ReactiveUI currently included is for WPF and so expects a WPF
            // dispatcher. This makes sure it's initialized.
#if !__ANDROID__
			 System.Windows.Threading.Dispatcher foo = System.Windows.Threading.Dispatcher.CurrentDispatcher;
#endif

            if (Application.Current.DataTemplates == null)
                Application.Current.DataTemplates = new DataTemplates();
            Application.Current.
                DataTemplates.Add(
                    new FuncTreeDataTemplate<Node>(
                        x => new TextBlock { Text = x.Name },
                        x => x.Children,
                        x => true)
                );

            TabControl container;

            Window window = new Window
            {
                Title = "Perspex Test Application",
                //Width = 900,
                //Height = 480,
                Content =
                new Grid
                {
                    Margin = DefaultMargin,
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
                            Padding = new Thickness(5*Scale),
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

            // window = TestUI.TestUIBuilder.BuildTestUI();
            Application.Current.Styles.Add(new SampleTabStyle());
            container.Classes.Add(":container");

            window.FontSize = window.FontSize * FontScale;
            return window;
        }

        public static Window BuildSimpleTestUI()
        {
            var window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content =
                        new Grid
                        {
                            //Width = 600 * Scale,
                            //ColumnDefinitions = new ColumnDefinitions
                            //                {
                            //        new ColumnDefinition(1, GridUnitType.Star),
                            //        new ColumnDefinition(1, GridUnitType.Star),
                            //                },

                            //RowDefinitions = new RowDefinitions
                            //                {
                            //        new RowDefinition(1, GridUnitType.Auto),
                            //        new RowDefinition(1, GridUnitType.Auto)
                            //                },
                            Children = new Controls
                                            {
                                    //new Rectangle
                                    //{
                                    //    Fill = SolidColorBrush.Parse("#FF5722"),
                                    //    [Grid.ColumnSpanProperty] = 2,
                                    //    Height = 200*Scale,
                                    //    Margin = new Thickness(2.5*Scale)
                                    //},
                                    //new Rectangle
                                    //{
                                    //    Fill = SolidColorBrush.Parse("#FF5722"),
                                    //    [Grid.RowProperty] = 1,
                                    //    Height = 100*Scale,
                                    //    Margin = new Thickness(2.5*Scale)
                                    //},
                                    //new Rectangle
                                    //{
                                    //    Fill = SolidColorBrush.Parse("#FF5722"),
                                    //    [Grid.RowProperty] = 1,
                                    //    [Grid.ColumnProperty] = 1,
                                    //    Height = 100*Scale,
                                    //    Margin = new Thickness(2.5*Scale)
                                    //},

                               new Path
                                    {
                                        Name = "checkMark",
                                        Fill = new SolidColorBrush(0xff333333),
                                        //Width = 20,
                                        //Height = 20,
                                        Stretch = Stretch.Uniform,
                                        HorizontalAlignment = HorizontalAlignment.Left,
                                        VerticalAlignment = VerticalAlignment.Top,
                                        Data = StreamGeometry.Parse("M 1145.607177734375,430 C1145.607177734375,430 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1138,434.5538330078125 1138,434.5538330078125 1138,434.5538330078125 1141.482177734375,438 1141.482177734375,438 1141.482177734375,438 1141.96875,437.9375 1141.96875,437.9375 1141.96875,437.9375 1147,431.34619140625 1147,431.34619140625 1147,431.34619140625 1145.607177734375,430 1145.607177734375,430 z"),
                                        //Data = StreamGeometry.Parse("M 114.5607177734375,43.0 C114.5607177734375,43.0 114.1449951171875,43.50772705078125 114.1449951171875,43.50772705078125 114.1449951171875,43.50772705078125 113.9232177734375,43.30999755859375 113.9232177734375,43.30999755859375 113.9232177734375,43.30999755859375 1138,43.45538330078125 113.8,43.45538330078125 113.8,43.45538330078125 114.1482177734375,43.8 114.1482177734375,438 114.1482177734375,43.8 114.196875,43.79375 114.196875,43.79375 114.196875,43.79375 114.7,43.134619140625 114.7,43.134619140625 114.7,43.134619140625 114.5607177734375,43.0 114.5607177734375,43.0 z"),
                                        [Grid.ColumnProperty] = 0,
                                    },
                       
                        },
            }
            };

            return window;
        }

    public static Window BuildTestUI()
    {
        Application.Current.Styles.Add(new Style(x => x.OfType<DropDown>())
        {
            Setters = new[]
                {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<DropDown>(DropDownControlTemplate)),
						//new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(0xff707070)),
						//new Setter(TemplatedControl.BorderThicknessProperty, 2.0),
						//new Setter(Control.FocusAdornerProperty, new FuncTemplate<IControl>(FocusAdornerTemplate)),
						//new Setter(DropDown.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
						//new Setter(DropDown.VerticalContentAlignmentProperty, VerticalAlignment.Center),
					},
        });

        //Application.Current.DataTemplates.Add(new FuncDataTemplate<string>(s =>
        //{
        //    return new TextBlock() { Text = s, FontSize = DefaultFontSize };
        //}));
        // TextPresenter
        Window result;
        //result = BuildSimpleTextBoxUI();
        //result = BuildSimpleTextUI();
        //result = BuildSimpleControlsUI();
        //return BuildStackPanelUI();
        //result = BuildListBoxUI();
        //result = BuildComboBoxUI();
        //result = BuildGridWithSomeButtonsAndStuff();
        result = CreateComplexTabUI();
       // result = BuildSimpleTestUI();
#if __ANDROID__
        //      DefaultFontSize = result.FontSize = result.FontSize * 2;
#endif
        return result;
    }
}

    internal class SampleTabStyle : Styles
    {
        public SampleTabStyle()
        {
            this.AddRange(new[]
            {
                new Style (s => s.Class(":container").OfType<TabControl> ())
                {
                    Setters = new[]
                    {
                        new Setter (TemplatedControl.TemplateProperty, new FuncControlTemplate<TabControl> (TabControlTemplate))
                    }
                },

                new Style(s => s.Class(":container").OfType<TabControl>().Child().Child().Child().Child().Child().OfType<TabItem>())
                {
                    Setters = new[]
                    {
                        new Setter (TemplatedControl.TemplateProperty, new FuncControlTemplate<TabItem> (TabItemTemplate)),
                    }
                },

                new Style(s => s.Name("internalStrip").OfType<TabStrip>().Child().OfType<TabItem>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.FontSizeProperty, 14.0),
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
                    }
                },

                new Style(s => s.Name("internalStrip").OfType<TabStrip>().Child().OfType<TabItem>().Class(":selected"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.White),
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Colors.White) { Opacity = 0.1 }),
                    },
                },
            });
        }

        public static Control TabItemTemplate(TabItem control)
        {
            return new ContentPresenter
            {
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<string>(x => new Border
                    {
                        [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                        Padding = new Thickness(10),
                        Child = new TextBlock
                        {
                            VerticalAlignment = Perspex.Layout.VerticalAlignment.Center,
                            Text = x
                        }
                    })
                },
                Name = "headerPresenter",
                [~ContentPresenter.ContentProperty] = control[~HeaderedContentControl.HeaderProperty],
            };
        }

        public static Control TabControlTemplate(TabControl control)
        {
            return new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                },
                Children = new Controls
                {
                    new Border
                    {
                        Width = 190,
                        Background = SolidColorBrush.Parse("#1976D2"),
                        Child = new ScrollViewer
                        {
                            Content = new TabStrip
                            {
                                ItemsPanel = new FuncTemplate<IPanel>(() => new StackPanel { Orientation = Orientation.Vertical, Gap = 4 }),
                                Margin = new Thickness(0, 10, 0, 0),
                                Name = "internalStrip",
                                [!ItemsControl.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                                [!!SelectingItemsControl.SelectedItemProperty] = control[!!SelectingItemsControl.SelectedItemProperty],
                            }
                        }
                    },
                    new Carousel
                    {
                        Name = "carousel",
                        MemberSelector = control.ContentSelector,
                        [~Carousel.TransitionProperty] = control[~TabControl.TransitionProperty],
                        [!Carousel.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                        [!Carousel.SelectedItemProperty] = control[!SelectingItemsControl.SelectedItemProperty],
                        [Grid.ColumnProperty] = 1,
                    }
                }
            };
        }
    }
}