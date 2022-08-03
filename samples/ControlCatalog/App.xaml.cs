using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Themes.Fluent;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public class App : Application
    {
        public App()
        {
            DataContext = new ApplicationViewModel();
        }

        public static readonly StyleInclude ColorPickerFluent = new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml")
        };

        public static readonly StyleInclude ColorPickerSimple = new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.ColorPicker/Themes/Simple/Simple.xaml")
        };

        public static readonly StyleInclude DataGridFluent = new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        };

        public static readonly StyleInclude DataGridSimple = new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
        };

        public static FluentTheme Fluent = new FluentTheme(new Uri("avares://ControlCatalog/Styles"));

        public static SimpleTheme Simple = new SimpleTheme(new Uri("avares://ControlCatalog/Styles"));

        public static Styles SimpleLight = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml")
            },
            Simple
        };

        public static Styles SimpleDark = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml")
            },
            Simple
        };

        public override void Initialize()
        {
            Styles.Insert(0, Fluent);
            Styles.Insert(1, ColorPickerFluent);
            Styles.Insert(2, DataGridFluent);
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.MainWindow = new MainWindow();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
            {
                singleViewLifetime.MainView = new MainView();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
