using System;
using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Default;

namespace Avalonia.AndroidTestApplication
{
    [Activity(Label = "Main",
        MainLauncher = true,
        Icon = "@drawable/icon",
        Theme = "@style/Theme.AppCompat.NoActionBar",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
        LaunchMode = LaunchMode.SingleInstance/*,
        ScreenOrientation = ScreenOrientation.Landscape*/)]
    public class MainActivity : AvaloniaActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder);
        }
    }

    public class App : Application
    {
        public override void Initialize()
        {
            Styles.Add(new DefaultTheme());

            var baseLight = (IStyle)AvaloniaXamlLoader.Load(
                new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default"));
            Styles.Add(baseLight);
        }

        // This provides a simple UI tree for testing input handling, drawing, etc
        public static ContentControl CreateSimpleWindow()
        {
            ContentControl window = new ContentControl()
            {
                Background = Brushes.Red,
                Content = new StackPanel
                {
                    Margin = new Thickness(30),
                    Background = Brushes.Yellow,
                    Children =
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
                        },

                        CreateTextBox(TextInputContentType.Normal),
                        CreateTextBox(TextInputContentType.Password),
                        CreateTextBox(TextInputContentType.Email),
                        CreateTextBox(TextInputContentType.Url),
                        CreateTextBox(TextInputContentType.Digits),
                        CreateTextBox(TextInputContentType.Number),
                    }
                }
            };

            return window;
        }

        private static TextBox CreateTextBox(TextInputContentType contentType)
        {
            var textBox = new TextBox()
            {
                Margin = new Thickness(20, 10),
                Watermark = contentType.ToString(),
                BorderThickness = new Thickness(3),
                FontSize = 20,
                [TextInputOptions.ContentTypeProperty] = contentType
            };

            return textBox;
        }
    }
}
