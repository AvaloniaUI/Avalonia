using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace ControlCatalog
{
    public class App : Application
    {
        public static Styles FluentDark = new Styles
        {
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")
            },
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/FluentDark.xaml")
            },
        };

        public static Styles FluentLight = new Styles
        {
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml")
            },
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/FluentLight.xaml")
            },
        };

        public static Styles DefaultLight = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default")
            },
        };

        public static Styles DefaultDark = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("resm:Avalonia.Themes.Default.Accents.BaseDark.xaml?assembly=Avalonia.Themes.Default")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default")
            },
        };

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            Styles.Insert(0, FluentLight);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                desktopLifetime.MainWindow = new MainWindow();
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
                singleViewLifetime.MainView = new MainView();

            base.OnFrameworkInitializationCompleted();
        }
    }
}
