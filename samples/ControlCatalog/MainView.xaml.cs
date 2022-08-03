using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using ControlCatalog.Models;
using ControlCatalog.Pages;

namespace ControlCatalog
{
    public class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);

            var sideBar = this.Get<TabControl>("Sidebar");

            if (AvaloniaLocator.Current?.GetService<IRuntimePlatform>()?.GetRuntimeInfo().IsDesktop == true)
            {
                IList tabItems = ((IList)sideBar.Items);
                tabItems.Add(new TabItem()
                {
                    Header = "Screens",
                    Content = new ScreenPage()
                });

            }

            var themes = this.Get<ComboBox>("Themes");
            themes.SelectionChanged += (sender, e) =>
            {
                if (themes.SelectedItem is CatalogTheme theme)
                {
                    var themeStyle = Application.Current.Styles[0];
                    if (theme == CatalogTheme.FluentLight)
                    {
                        if (App.Fluent.Mode != FluentThemeMode.Light)
                        {
                            App.Fluent.Mode = FluentThemeMode.Light;
                        }
                        Application.Current.Styles[0] = App.Fluent;
                        Application.Current.Styles[1] = App.ColorPickerFluent;
                        Application.Current.Styles[2] = App.DataGridFluent;
                    }
                    else if (theme == CatalogTheme.FluentDark)
                    {

                        if (App.Fluent.Mode != FluentThemeMode.Dark)
                        {
                            App.Fluent.Mode = FluentThemeMode.Dark;
                        }
                        Application.Current.Styles[0] = App.Fluent;
                        Application.Current.Styles[1] = App.ColorPickerFluent;
                        Application.Current.Styles[2] = App.DataGridFluent;
                    }
                    else if (theme == CatalogTheme.DefaultLight)
                    {
                        App.Default.Mode = Avalonia.Themes.Simple.SimpleThemeMode.Light;
                        Application.Current.Styles[0] = App.DefaultLight;
                        Application.Current.Styles[1] = App.ColorPickerDefault;
                        Application.Current.Styles[2] = App.DataGridDefault;
                    }
                    else if (theme == CatalogTheme.DefaultDark)
                    {
                        App.Default.Mode = Avalonia.Themes.Simple.SimpleThemeMode.Dark;
                        Application.Current.Styles[0] = App.DefaultDark;
                        Application.Current.Styles[1] = App.ColorPickerDefault;
                        Application.Current.Styles[2] = App.DataGridDefault;
                    }
                }
            };

            var flowDirections = this.Get<ComboBox>("FlowDirection");
            flowDirections.SelectionChanged += (sender, e) =>
            {
                if (flowDirections.SelectedItem is FlowDirection flowDirection)
                {
                    this.FlowDirection = flowDirection;
                }
            };

            var decorations = this.Get<ComboBox>("Decorations");
            decorations.SelectionChanged += (sender, e) =>
            {
                if (VisualRoot is Window window
                    && decorations.SelectedItem is SystemDecorations systemDecorations)
                {
                    window.SystemDecorations = systemDecorations;
                }
            };

            var transparencyLevels = this.Get<ComboBox>("TransparencyLevels");
            IDisposable? backgroundSetter = null, paneBackgroundSetter = null;
            transparencyLevels.SelectionChanged += (sender, e) =>
            {
                backgroundSetter?.Dispose();
                paneBackgroundSetter?.Dispose();
                if (transparencyLevels.SelectedItem is WindowTransparencyLevel selected
                    && selected != WindowTransparencyLevel.None)
                {
                    var semiTransparentBrush = new ImmutableSolidColorBrush(Colors.Gray, 0.5);
                    backgroundSetter = sideBar.SetValue(BackgroundProperty, semiTransparentBrush, Avalonia.Data.BindingPriority.Style);
                    paneBackgroundSetter = sideBar.SetValue(SplitView.PaneBackgroundProperty, semiTransparentBrush, Avalonia.Data.BindingPriority.Style);
                }
            };
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var decorations = this.Get<ComboBox>("Decorations");
            if (VisualRoot is Window window)
                decorations.SelectedIndex = (int)window.SystemDecorations;
        }
    }
}
