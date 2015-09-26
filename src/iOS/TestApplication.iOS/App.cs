using Perspex;
using Perspex.Animation;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.iOS;
using Perspex.Media;
using System;


namespace TestApplication.iOS
{
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

        public void BuildAndRun()
        {
            //TabControl container;

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
                        new Rectangle
                        {
                            Width = 200,
                            Height = 200,
                            Fill = Brushes.Yellow
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


