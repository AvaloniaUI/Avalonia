using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages;

public class PopupPage : UserControl
{
    public PopupPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

