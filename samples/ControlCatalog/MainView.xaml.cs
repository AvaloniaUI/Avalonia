using System;
using System.Collections;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Avalonia.Styling;
using ControlCatalog.Models;
using ControlCatalog.Pages;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public class MainView : UserControl
    {
        private readonly IPlatformSettings _platformSettings;

        public MainView()
        {
            AvaloniaXamlLoader.Load(this);
            _platformSettings = AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>();
            PlatformSettingsOnColorValuesChanged(_platformSettings, _platformSettings.GetColorValues());
            
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
                    App.SetCatalogThemes(theme);
                }
            };
            var themeVariants = this.Get<ComboBox>("ThemeVariants");
            themeVariants.SelectedItem = Application.Current!.RequestedThemeVariant;
            themeVariants.SelectionChanged += (sender, e) =>
            {
                if (themeVariants.SelectedItem is ThemeVariant themeVariant)
                {
                    Application.Current!.RequestedThemeVariant = themeVariant;
                }
            };

            var flowDirections = this.Get<ComboBox>("FlowDirection");
            flowDirections.SelectionChanged += (sender, e) =>
            {
                if (flowDirections.SelectedItem is FlowDirection flowDirection)
                {
                    TopLevel.GetTopLevel(this).FlowDirection = flowDirection;
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

        internal MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var decorations = this.Get<ComboBox>("Decorations");
            if (VisualRoot is Window window)
                decorations.SelectedIndex = (int)window.SystemDecorations;

            var insets = TopLevel.GetTopLevel(this)!.InsetsManager;
            if (insets != null)
            {
                // In real life application these events should be unsubscribed to avoid memory leaks.
                ViewModel.SafeAreaPadding = insets.SafeAreaPadding;
                insets.SafeAreaChanged += (sender, args) =>
                {
                    ViewModel.SafeAreaPadding = insets.SafeAreaPadding;
                };

                ViewModel.DisplayEdgeToEdge = insets.DisplayEdgeToEdge;
                ViewModel.IsSystemBarVisible = insets.IsSystemBarVisible ?? true;

                ViewModel.PropertyChanged += async (sender, args) =>
                {
                    if (args.PropertyName == nameof(ViewModel.DisplayEdgeToEdge))
                    {
                        insets.DisplayEdgeToEdge = ViewModel.DisplayEdgeToEdge;
                    }
                    else if (args.PropertyName == nameof(ViewModel.IsSystemBarVisible))
                    {
                        insets.IsSystemBarVisible = ViewModel.IsSystemBarVisible;
                    }

                    // Give the OS some time to apply new values and refresh the view model.
                    await Task.Delay(100);
                    ViewModel.DisplayEdgeToEdge = insets.DisplayEdgeToEdge;
                    ViewModel.IsSystemBarVisible = insets.IsSystemBarVisible ?? true;
                };
            }

            _platformSettings.ColorValuesChanged += PlatformSettingsOnColorValuesChanged;
            PlatformSettingsOnColorValuesChanged(_platformSettings, _platformSettings.GetColorValues());
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            
            _platformSettings.ColorValuesChanged -= PlatformSettingsOnColorValuesChanged;
        }

        private void PlatformSettingsOnColorValuesChanged(object? sender, PlatformColorValues e)
        {
            Application.Current!.Resources["SystemAccentColor"] = e.AccentColor1;
            Application.Current.Resources["SystemAccentColorDark1"] = ChangeColorLuminosity(e.AccentColor1, -0.3);
            Application.Current.Resources["SystemAccentColorDark2"] = ChangeColorLuminosity(e.AccentColor1, -0.5);
            Application.Current.Resources["SystemAccentColorDark3"] = ChangeColorLuminosity(e.AccentColor1, -0.7);
            Application.Current.Resources["SystemAccentColorLight1"] = ChangeColorLuminosity(e.AccentColor1, 0.3);
            Application.Current.Resources["SystemAccentColorLight2"] = ChangeColorLuminosity(e.AccentColor1, 0.5);
            Application.Current.Resources["SystemAccentColorLight3"] = ChangeColorLuminosity(e.AccentColor1, 0.7);

            static Color ChangeColorLuminosity(Color color, double luminosityFactor)
            {
                var red = (double)color.R;
                var green = (double)color.G;
                var blue = (double)color.B;

                if (luminosityFactor < 0)
                {
                    luminosityFactor = 1 + luminosityFactor;
                    red *= luminosityFactor;
                    green *= luminosityFactor;
                    blue *= luminosityFactor;
                }
                else if (luminosityFactor >= 0)
                {
                    red = (255 - red) * luminosityFactor + red;
                    green = (255 - green) * luminosityFactor + green;
                    blue = (255 - blue) * luminosityFactor + blue;
                }

                return new Color(color.A, (byte)red, (byte)green, (byte)blue);
            }
        }
    }
}
