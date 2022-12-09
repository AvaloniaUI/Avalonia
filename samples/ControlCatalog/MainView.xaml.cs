using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
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
            IDisposable? topLevelBackgroundSideSetter = null, sideBarBackgroundSetter = null, paneBackgroundSetter = null;
            transparencyLevels.SelectionChanged += (sender, e) =>
            {
                topLevelBackgroundSideSetter?.Dispose();
                sideBarBackgroundSetter?.Dispose();
                paneBackgroundSetter?.Dispose();
                if (transparencyLevels.SelectedItem is WindowTransparencyLevel selected)
                {
                    var topLevel = (TopLevel)this.GetVisualRoot()!;
                    topLevel.TransparencyLevelHint = selected;

                    if (selected != WindowTransparencyLevel.None)
                    {
                        var transparentBrush = new ImmutableSolidColorBrush(Colors.White, 0);
                        var semiTransparentBrush = new ImmutableSolidColorBrush(Colors.Gray, 0.2);
                        topLevelBackgroundSideSetter = topLevel.SetValue(BackgroundProperty, transparentBrush, Avalonia.Data.BindingPriority.Style);
                        sideBarBackgroundSetter = sideBar.SetValue(BackgroundProperty, semiTransparentBrush, Avalonia.Data.BindingPriority.Style);
                        paneBackgroundSetter = sideBar.SetValue(SplitView.PaneBackgroundProperty, semiTransparentBrush, Avalonia.Data.BindingPriority.Style);
                    }
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
