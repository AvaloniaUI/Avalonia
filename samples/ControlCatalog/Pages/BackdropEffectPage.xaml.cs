using Avalonia.Controls;
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
}
