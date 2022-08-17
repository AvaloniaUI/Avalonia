using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace AotSandbox;

public class App : Application
{
    public App()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            TextBlock tb = null;
            Button b = null;
            desktop.MainWindow = new MyWindow
            {
                Content = new StackPanel()
                {
                    
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 10,
                    Children =
                    {
                        (tb = new TextBlock()),
                        (b = new Button()
                        {
                            Content = "Hello world!",
                        })
                    }
                }
            };
            int cnt = 0;
            void Update() => tb.Text = "Clicked " + cnt + " times";
            b.Click+= delegate
            {
                cnt++;
                Update();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    class MyWindow : Window
    {

    }
}