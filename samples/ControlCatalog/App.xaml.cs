using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Themes.Fluent;
using ControlCatalog.Models;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public class App : Application
    {
        private readonly Styles _themeStylesContainer = new();
        private FluentTheme? _fluentTheme;
        private SimpleTheme? _simpleTheme;
        private IResourceDictionary? _fluentBaseLightColors, _fluentBaseDarkColors;
        private IStyle? _colorPickerFluent, _colorPickerSimple;
        private IStyle? _dataGridFluent, _dataGridSimple;
        
        public App()
        {
            DataContext = new ApplicationViewModel();
        }

        public override void Initialize()
        {
            Styles.Add(_themeStylesContainer);

            AvaloniaXamlLoader.Load(this);

            _fluentTheme = new FluentTheme();
            _simpleTheme = new SimpleTheme();
            _simpleTheme.Resources.MergedDictionaries.Add((IResourceDictionary)Resources["FluentAccentColors"]!);
            _simpleTheme.Resources.MergedDictionaries.Add((IResourceDictionary)Resources["FluentBaseColors"]!);
            _colorPickerFluent = (IStyle)Resources["ColorPickerFluent"]!;
            _colorPickerSimple = (IStyle)Resources["ColorPickerSimple"]!;
            _dataGridFluent = (IStyle)Resources["DataGridFluent"]!;
            _dataGridSimple = (IStyle)Resources["DataGridSimple"]!;
            _fluentBaseLightColors = (IResourceDictionary)Resources["FluentBaseLightColors"]!;
            _fluentBaseDarkColors = (IResourceDictionary)Resources["FluentBaseDarkColors"]!;
            
            SetThemeVariant(CatalogTheme.FluentLight);
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

        private CatalogTheme _prevTheme;
        public static CatalogTheme CurrentTheme => ((App)Current!)._prevTheme; 
        public static void SetThemeVariant(CatalogTheme theme)
        {
            var app = (App)Current!;
            var prevTheme = app._prevTheme;
            app._prevTheme = theme;
            var shouldReopenWindow = theme switch
            {
                CatalogTheme.FluentLight => prevTheme is CatalogTheme.SimpleDark or CatalogTheme.SimpleLight,
                CatalogTheme.FluentDark => prevTheme is CatalogTheme.SimpleDark or CatalogTheme.SimpleLight,
                CatalogTheme.SimpleLight => prevTheme is CatalogTheme.FluentDark or CatalogTheme.FluentLight,
                CatalogTheme.SimpleDark => prevTheme is CatalogTheme.FluentDark or CatalogTheme.FluentLight,
                _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
            };
            
            if (app._themeStylesContainer.Count == 0)
            {
                app._themeStylesContainer.Add(new Style());
                app._themeStylesContainer.Add(new Style());
                app._themeStylesContainer.Add(new Style());
            }
            
            if (theme == CatalogTheme.FluentLight)
            {
                app._fluentTheme!.Mode = FluentThemeMode.Light;
                app._themeStylesContainer[0] = app._fluentTheme;
                app._themeStylesContainer[1] = app._colorPickerFluent!;
                app._themeStylesContainer[2] = app._dataGridFluent!;
            }
            else if (theme == CatalogTheme.FluentDark)
            {
                app._fluentTheme!.Mode = FluentThemeMode.Dark;
                app._themeStylesContainer[0] = app._fluentTheme;
                app._themeStylesContainer[1] = app._colorPickerFluent!;
                app._themeStylesContainer[2] = app._dataGridFluent!;
            }
            else if (theme == CatalogTheme.SimpleLight)
            {
                app._simpleTheme!.Mode = SimpleThemeMode.Light;
                app._simpleTheme.Resources.MergedDictionaries.Remove(app._fluentBaseDarkColors!);
                app._simpleTheme.Resources.MergedDictionaries.Add(app._fluentBaseLightColors!);
                app._themeStylesContainer[0] = app._simpleTheme;
                app._themeStylesContainer[1] = app._colorPickerSimple!;
                app._themeStylesContainer[2] = app._dataGridSimple!;
            }
            else if (theme == CatalogTheme.SimpleDark)
            {
                app._simpleTheme!.Mode = SimpleThemeMode.Dark;
                app._simpleTheme.Resources.MergedDictionaries.Remove(app._fluentBaseLightColors!);
                app._simpleTheme.Resources.MergedDictionaries.Add(app._fluentBaseDarkColors!);
                app._themeStylesContainer[0] = app._simpleTheme;
                app._themeStylesContainer[1] = app._colorPickerSimple!;
                app._themeStylesContainer[2] = app._dataGridSimple!;
            }

            if (shouldReopenWindow)
            {
                if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    var oldWindow = desktopLifetime.MainWindow;
                    var newWindow = new MainWindow();
                    desktopLifetime.MainWindow = newWindow;
                    newWindow.Show();
                    oldWindow?.Close();
                }
                else if (app.ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
                {
                    singleViewLifetime.MainView = new MainView();
                }
            }
        }
    }
}
