using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android.Platform.Specific;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Default;

namespace Avalonia.AndroidTestApplication
{
    [Activity(Label = "Main",
        MainLauncher = true,
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleInstance/*,
        ScreenOrientation = ScreenOrientation.Landscape*/)]
    public class MainBaseActivity : AvaloniaActivity
    {
        public MainBaseActivity() : base(typeof (App))
        {

        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            App app;
            if (Avalonia.Application.Current != null)
                app = (App)Avalonia.Application.Current;
            else
            {
                app = new App();
                AppBuilder.Configure(app)
                    .UseAndroid()
                    .UseSkia()
                    .SetupWithoutStarting();
            }

            app.Run();
        }
    }

    public class App : Application
    {
        public void Run()
        {
            Styles.Add(new DefaultTheme());

            var loader = new AvaloniaXamlLoader();
            var baseLight = (IStyle)loader.Load(
                new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default"));
            Styles.Add(baseLight);

            var wnd = App.CreateSimpleWindow();
            wnd.AttachDevTools();

            Run(wnd);
        }

        // This provides a simple UI tree for testing input handling, drawing, etc
        public static Window CreateSimpleWindow()
        {
            Window window = new Window
            {
                Title = "Avalonia Test Application",
                Background = Brushes.Red,
                Content = new StackPanel
                {
                    Margin = new Thickness(30),
                    Background = Brushes.Yellow,
                    Children = new Avalonia.Controls.Controls
                    {
                        new TextBlock
                        {
                            Text = "TEXT BLOCK",
                            Width = 300,
                            Height = 40,
                            Background = Brushes.White,
                            Foreground = Brushes.Black
                        },

                        new Button
                        {
                            Content = "BUTTON",
                            Width = 150,
                            Height = 40,
                            Background = Brushes.LightGreen,
                            Foreground = Brushes.Black
                        }

                    }
                }
            };

            return window;
        }
    }
}
