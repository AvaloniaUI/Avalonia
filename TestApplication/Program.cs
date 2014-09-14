using System.Collections;
using System.Collections.Generic;
using Perspex;
using Perspex.Controls;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.Threading;
using Perspex.Windows;
using Splat;

namespace TestApplication
{
    class TestLogger : ILogger
    {
        public LogLevel Level
        {
            get;
            set;
        }

        public void Write(string message, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level) return;
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    class Item
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    class Node
    {
        public string Name { get; set; }
        public IEnumerable<Node> Children { get; set; }
    }

    class Program
    {
        private static Node[] treeData = new[]
        {
            new Node
            {
                Name = "Root 1",
                Children = new[]
                {
                    new Node
                    {
                        Name = "Child 1",
                    },
                    new Node
                    {
                        Name = "Child 2",
                        Children = new[]
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

        static void Main(string[] args)
        {
            //Locator.CurrentMutable.Register(() => new TestLogger { Level = LogLevel.Debug } , typeof(ILogger));

            App application = new App
            {
                DataTemplates = new DataTemplates
                {
                    new TreeDataTemplate<Node>(
                        x => new TextBlock { Text = x.Name },
                        x => x.Children),
                },
            };

            Window window = new Window
            {
                Content = new TabControl
                {
                    Items = new[]
                    {
                        new TabItem
                        {
                            Header = "Buttons",
                            Content = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Gap = 8,
                                MinWidth = 120,
                                Children = new Controls
                                {
                                    new Button
                                    {
                                        Content = "Button",
                                    },
                                    new Button
                                    {
                                        Content = "Button",
                                        Background = new SolidColorBrush(0xcc119eda),
                                    },
                                    new CheckBox
                                    {
                                        Content = "Checkbox",
                                    },
                                }
                            },
                        },
                        new TabItem
                        {
                            Header = "Text",
                            Content = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Gap = 8,
                                Width = 120,
                                Children = new Controls
                                {
                                    new TextBlock
                                    {
                                        Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin venenatis dui quis libero suscipit tincidunt.",
                                    },
                                    new TextBox
                                    {
                                        Text = "Text Box",
                                    },
                                }
                            },
                        },
                        new TabItem
                        {
                            Header = "Images",
                            Content = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Gap = 8,
                                Width = 120,
                                Children = new Controls
                                {
                                    new Image
                                    {
                                        Source = new Bitmap("github_icon.png"),
                                        Width = 200,
                                    },
                                }
                            },
                        },
                        new TabItem
                        {
                            Header = "Lists",
                            Content = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Gap = 8,
                                Width = 120,
                                Children = new Controls
                                {
                                    new TreeView
                                    {
                                        Items = treeData,
                                    },
                                }
                            },
                        },
                    }
                }
            };

            System.Console.WriteLine(Perspex.Diagnostics.Debug.PrintVisualTree(window));

            window.Show();
            Dispatcher.Run();
        }
    }
}
