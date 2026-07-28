using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AppWithoutLifetime;

public partial class Sub : Window
{
    public Sub()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
