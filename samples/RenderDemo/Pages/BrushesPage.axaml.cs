using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RenderDemo.Pages;

public class BrushesPage : UserControl
{
    public BrushesPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

