using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Avalonia.Controls;
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
        protected override void OnCreate(Bundle savedInstanceState)
        {
            if (Avalonia.Application.Current == null)
            {
                AppBuilder.Configure<App>()
                    .UseAndroid()
                    .SetupWithoutStarting();
            }
            base.OnCreate(savedInstanceState);
            Content = App.CreateSimpleWindow();
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

                        CreateTextBox(Input.TextInput.TextInputContentType.Normal),
                        CreateTextBox(Input.TextInput.TextInputContentType.Password),
                        CreateTextBox(Input.TextInput.TextInputContentType.Email),
                        CreateTextBox(Input.TextInput.TextInputContentType.Url),
                        CreateTextBox(Input.TextInput.TextInputContentType.Phone),
                        CreateTextBox(Input.TextInput.TextInputContentType.Number),
                    }
                }
            };

            return window;
        }

        private static TextBox CreateTextBox(Input.TextInput.TextInputContentType contentType)
        {
            var textBox = new TextBox()
            {
                Margin = new Thickness(20, 10),
                Watermark = contentType.ToString(),
                BorderThickness = new Thickness(3),
                FontSize = 20
            };
            textBox.TextInputOptionsQuery += (s, e) => { e.ContentType = contentType; };

            return textBox;
        }
    }
}
