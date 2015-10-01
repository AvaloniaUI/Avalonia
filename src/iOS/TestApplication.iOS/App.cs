using Perspex;
using Perspex.Animation;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.iOS;
using Perspex.Media;
using Serilog;
using System;

// We should move this sink implementation elsewhere, for now I need this here for iOS dev
// the other question is, is there another sink already part of Serilog we could use?
namespace TestApplication.iOS
{
    using Serilog;
    using Serilog.Configuration;
    using Serilog.Core;
    using Serilog.Events;
    using Serilog.Formatting;
    using Serilog.Formatting.Display;
    using System.IO;

    // Copied this from Serilog.MonoTouch package that refuses to install for some reason
    //

    class NSLogSink : ILogEventSink
    {
        readonly ITextFormatter _textFormatter;

        public NSLogSink(ITextFormatter textFormatter)
        {
            if (textFormatter == null) throw new ArgumentNullException("textFormatter");
            _textFormatter = textFormatter;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException("logEvent");
            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);
            Console.WriteLine(renderSpace.ToString());
        }
    }

    public static class LoggerConfigurationMonoTouchExtensions
    {
        const string DefaultNSLogOutputTemplate = "[{Level}] {Message:l{NewLine:l}{Exception:l}";

        /// <summary>
        /// Adds a sink that writes log events to a Azure DocumentDB table in the provided endpoint.
        /// </summary>
        /// <param name="sinkConfiguration">The configuration being modified.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="outputTemplate">Template for the output events</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration NSLog(this LoggerSinkConfiguration sinkConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultNSLogOutputTemplate,
            IFormatProvider formatProvider = null)
        {

            if (sinkConfiguration == null)
                throw new ArgumentNullException("sinkConfiguration");

            if (outputTemplate == null)
                throw new ArgumentNullException("outputTemplate");

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return sinkConfiguration.Sink(new NSLogSink(formatter), restrictedToMinimumLevel);
        }
    }
}

namespace TestApplication.iOS
{
    internal class Item
    {
        public string Name { get; set; }
        public string Value { get; set; }
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

    // This should be moved into a shared project across all platforms???
    public class App : Perspex.Application
    {
        public App()
        {
            RegisterServices();
            InitializePlatform();

            Styles = new Perspex.Themes.Default.DefaultTheme();

            //DataTemplates = new DataTemplates
            //{
            //    new TreeDataTemplate<Node>(
            //        x => new TextBlock { Text = x.Name },
            //        x => x.Children,
            //        x => true),
            //},
        }

        // ?? Perhaps we move this to PlatformSupport so iOS can have it's own implementation
        //

        /// <summary>
        /// Initializes the rendering or windowing subsystem defined by the specified assembly.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly.</param>
        protected void InitializePlatform()
        {
            // Setup logging which will be useful for tracking certain issues
            Log.Logger = new LoggerConfiguration()
                .WriteTo.NSLog()
                .CreateLogger();

            // on iOS due to AOT we cannot dynamically load an assembly
            //
            //var assembly = Assembly.Load(new AssemblyName(assemblyName));
            //var platformClassName = assemblyName.Replace("Perspex.", string.Empty) + "Platform";
            //var platformClassFullName = assemblyName + "." + platformClassName;
            //var platformClass = assembly.GetType(platformClassFullName);
            //var init = platformClass.GetRuntimeMethod("Initialize", new Type[0]);
            //init.Invoke(null, null);

            // just call init method directly
            iOSPlatform.Initialize();
        }

        public void BuildSimpleTextUI()
        {
            Window window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new TextBlock
                {
                    Text = "How does this look? Is this text going to wrap and then look nice within the bounds of this widget? If not I will be extremely disappointed!\n\nWill we start a new paragraph here? If not there will be hell to pay!!!!",
                    Foreground = Brushes.White,
                    Background = Brushes.SteelBlue,
                    //FontFamily = ""       // what is available on iOS??? 
                    FontSize = 12,
                    Margin = new Thickness(50),
                    //TextAlignment = TextAlignment.Center
                }
            };

            window.Show();
        }

        private static readonly PerspexList<Item> s_listBoxData = new PerspexList<Item>
        {
            new Item { Name = "Item 1", Value = "Value 1" },
            new Item { Name = "Item 2", Value = "Value 2" },
            new Item { Name = "Item 3", Value = "Value 3" },
            new Item { Name = "Item 4", Value = "Value 4" },
            new Item { Name = "Item 5", Value = "Value 5" },
            new Item { Name = "Item 6", Value = "Value 6" },
            new Item { Name = "Item 7", Value = "Value 7" },
            new Item { Name = "Item 8", Value = "Value 8" },
        };

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

        public Window BuildListTest()
        {
            Window window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    DataTemplates = new DataTemplates
                    {
                        new DataTemplate<Item>(x =>
                            new StackPanel
                            {
                                Gap = 4,
                                Orientation = Orientation.Horizontal,
                                Children = new Controls
                                {
                                    //new Image { Width = 50, Height = 50, Source = new Bitmap("github_icon.png") },
                                    new TextBlock { Text = x.Name, FontSize = 18 },
                                    new TextBlock { Text = x.Value, FontSize = 18 }
                                }
                            })
                    },

                    Margin = new Thickness(0, 20, 0, 0),    // skip the status bar area on iOS
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(60, GridUnitType.Pixel),
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(1, GridUnitType.Star),
                    },
                    Children = new Controls
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            [Grid.RowProperty] = 0,
                            Background = SolidColorBrush.Parse("#000000"),
                            Children = new Controls
                            {
                                new Button
                                {
                                    Content = "Button 1",
                                    Width = 100,
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
                        },

                        new ListBox
                        {
                            [Grid.RowProperty] = 1,
                            BorderThickness = 2,
                            Items = s_listBoxData,
                            Height = 300,
                            Width =  300,
                        },

                        new TreeView
                        {
                            [Grid.RowProperty] = 2,
                            Name = "treeView",
                            Items = s_treeData,
                            Height = 300,
                            BorderThickness = 2,
                            Width =  300,
                        }
                    }
                }
            };

            return window;
        }

        public void BuildGridWithSomeButtonsAndStuff()
        {
            //TabControl container;

            Window window = new Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    Margin = new Thickness(0, 20, 0, 0),    // skip the status bar area on iOS
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star),
                    },
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(40, GridUnitType.Pixel),
                        new RowDefinition(80, GridUnitType.Pixel),
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(1, GridUnitType.Star),
                    },
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            [Grid.RowProperty] = 0,
                            [Grid.ColumnSpanProperty] = 2,
                            Text = "Welcome to Perspex on iOS !!!",
                            Foreground = Brushes.SteelBlue,
                            Background = Brushes.White,
                            FontSize = 22,
                            Margin = new Thickness(5,7,5,5),
                            TextAlignment = TextAlignment.Center
                        },

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
                        },

                        new Rectangle
                        {
                            [Grid.RowProperty] = 2,
                            [Grid.ColumnProperty] = 0,
                            Margin = new Thickness(20),
                            Fill = Brushes.Red
                        },

                        new Ellipse
                        {
                            [Grid.RowProperty] = 3,
                            [Grid.ColumnProperty] = 0,
                            Margin = new Thickness(20),
                            Fill = Brushes.Blue
                        },

                        new TextBlock
                        {
                            [Grid.RowProperty] = 2,
                            [Grid.ColumnProperty] = 1,
                            Text = "How does this look? Is this text going to wrap and then look nice within the bounds of this widget? If not I will be extremely disappointed!\n\nWill we start a new paragraph here? If not there will be hell to pay!!!!",
                            Foreground = Brushes.White,
                            Background = Brushes.Transparent,
                            FontSize = 14,
                            Margin = new Thickness(10,30,10,10),
                        },

                        new Path
                        {
                            Data = StreamGeometry.Parse("M 50,50 l 15,0 l 5,-15 l 5,15 l 15,0 l -10,10 l 4,15 l -15,-9 l -15,9 l 7,-15 Z"),
                            [Grid.RowProperty] = 3,
                            [Grid.ColumnProperty] = 1,
                            Margin = new Thickness(20),
                            Fill = Brushes.White,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 4
                        }


                        //(container = new TabControl
                        //{
                        //    Padding = new Thickness(5),
                        //    Items = new[]
                        //    {
                        //        ButtonsTab(),
                        //        //TextTab(),
                        //        //HtmlTab(),
                        //        //ImagesTab(),
                        //        //ListsTab(),
                        //        //LayoutTab(),
                        //        //AnimationsTab(),
                        //    },
                        //    Transition = new PageSlide(TimeSpan.FromSeconds(0.25)),
                        //    [Grid.RowProperty] = 1,
                        //    [Grid.ColumnSpanProperty] = 2,
                        //})
                    }
                },
            };

            //container.Classes.Add(":container");

            window.Show();

            // this is now safe to call, and would be what we do in a cross-platform application
            Perspex.Application.Current.Run(window);
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
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A button control",
                                FontSize = 13,
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
                                Margin = new Thickness(0, 40, 0, 0),
                                Text = "ToggleButton",
                                FontWeight = FontWeight.Medium,
                                FontSize = 20,
                                Foreground = SolidColorBrush.Parse("#212121"),
                            },
                            new TextBlock
                            {
                                Text = "A toggle button control",
                                FontSize = 13,
                                Foreground = SolidColorBrush.Parse("#727272"),
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
    }
}


