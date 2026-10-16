using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using ControlCatalog.Models;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly TransparentStyles _transparentStyles = new();

        public SettingsPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();

            DataContext = settingsViewModel;
        }

        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is SettingsViewModel viewModel)
            {
                var topLevel = TopLevel.GetTopLevel(this)!;
                if (topLevel is Window window)
                    viewModel.SelectedDecorationIndex = (int)window.WindowDecorations;
            }
        }

        private void Decorations_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window window && e.AddedItems.Count > 0 && e.AddedItems[0] is WindowDecorations systemDecorations)
            {
                window.WindowDecorations = systemDecorations;
            }
        }

        private void Themes_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is CatalogTheme theme)
            {
                App.SetCatalogThemes(theme);
            }
        }

        private void ThemeVariants_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Application.Current is { } app && e.AddedItems.Count > 0 && e.AddedItems[0] is ThemeVariant themeVariant)
            {
                app.RequestedThemeVariant = themeVariant;
            }
        }

        private void FlowDirection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is { } topLevel && e.AddedItems.Count > 0 && e.AddedItems[0] is FlowDirection flowDirection)
            {
                topLevel.FlowDirection = flowDirection;
            }
        }

        private void TransparencyLevels_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is { } topLevel && e.AddedItems.Count > 0 && e.AddedItems[0] is WindowTransparencyLevel transparencyLevel)
            {
                topLevel.TransparencyLevelHint = [transparencyLevel];

                if (topLevel.ActualTransparencyLevel != WindowTransparencyLevel.None &&
                    transparencyLevel != WindowTransparencyLevel.None)
                {
                    topLevel.Background = new ImmutableSolidColorBrush(Colors.Gray, 0.2);
                    if (!topLevel.Styles.Contains(_transparentStyles))
                        topLevel.Styles.Add(_transparentStyles);
                }
                else
                {
                    topLevel.Background = null;
                    topLevel.Styles.Remove(_transparentStyles);
                }
            }
        }
    }
}
