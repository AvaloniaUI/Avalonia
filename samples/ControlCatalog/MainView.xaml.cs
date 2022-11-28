using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                var tabItems = (sideBar.Items as IList);
                tabItems?.Add(new TabItem()
                {
                    Header = "Screens",
                    Content = new ScreenPage()
                });
            }

            var themes = this.Get<ComboBox>("Themes");
            themes.SelectedItem = App.CurrentTheme;
            themes.SelectionChanged += (sender, e) =>
            {
                if (themes.SelectedItem is CatalogTheme theme)
                {
                    App.SetThemeVariant(theme);
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
