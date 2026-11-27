using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class BackdropEffectPage : ContentPage
{
    public BackdropEffectPage()
    {
        InitializeComponent();
    }

    private void CacheMode_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Raised while the XAML is still loading, before the named controls are assigned.
        if (Frosted is null || sender is not ComboBox { SelectedItem: ComboBoxItem { Content: string mode } })
            return;

        Frosted.BackdropEffectCache = BackdropEffectCache.Parse(mode);
    }

    private void MaskedCacheMode_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Raised while the XAML is still loading, before the named controls are assigned.
        if (MaskedFrosted is null || sender is not ComboBox { SelectedItem: ComboBoxItem { Content: string mode } })
            return;

        MaskedFrosted.BackdropEffectCache = BackdropEffectCache.Parse(mode);
    }

    private void MaskToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        // Raised while the XAML is still loading, before the named controls are assigned.
        if (MaskedScroll is null)
            return;

        // Removing the mask also removes the intermediate layer it opens, so the blur can be compared with
        // and without it.
        MaskedScroll.OpacityMask = MaskToggle.IsChecked == true ? CreateEdgeFadeMask() : null;
    }

    // Fades the ScrollViewer's content out at the top and the bottom edges.
    private static IBrush CreateEdgeFadeMask() => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(0, 0, 0, 0), 0),
            new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.15),
            new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.85),
            new GradientStop(Color.FromArgb(0, 0, 0, 0), 1),
        }
    };
}
