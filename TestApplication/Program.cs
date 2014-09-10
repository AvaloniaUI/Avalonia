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

    class Program
    {
        static void Main(string[] args)
        {
            App application = new App();

            Locator.CurrentMutable.Register(() => new TestLogger { Level = LogLevel.Debug } , typeof(ILogger));

            Window window = new Window
            {
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Orientation = Orientation.Vertical,
                    Gap = 6,
                    Children = new PerspexList<Control>
                    {
                        new Button
                        {
                            Content = "Button",
                        },
                        new Button
                        {
                            Content = "Explict Background",
                            Background = new SolidColorBrush(0xffa0a0ff),
                        },
                        new CheckBox
                        {
                            Content = "Checkbox",
                        },
                        new TextBox
                        {
                            Text = "Hello World!",
                        },
                        new Image
                        {
                            Source = new Bitmap("github_icon.png"),
                            Width = 200,
                        },
                        new ItemsControl
                        {
                            DataTemplates = new DataTemplates
                            {
                                new DataTemplate<Item>(o => new Border
                                {
                                    Background = Brushes.Red,
                                    BorderBrush = Brushes.Black,
                                    BorderThickness = 1,
                                    Content = new StackPanel
                                    {
                                        Orientation = Orientation.Vertical,
                                        Children = new PerspexList<Control>
                                        {
                                            new TextBlock
                                            {
                                                Text = o.Name,
                                            },
                                            new TextBlock
                                            {
                                                Text = o.Value,
                                            }
                                        },
                                    }
                                }),
                            },
                            Items = new[]
                            {
                                new Item { Name = "Foo", Value = "Bar" },
                                new Item { Name = "Buzz", Value = "Aldrin" },
                            },
                        }
                    }
                }
            };

            window.Show();
            Dispatcher.Run();
        }
    }
}
