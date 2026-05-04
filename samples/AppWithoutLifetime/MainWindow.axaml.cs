using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AppWithoutLifetime;

public partial class MainWindow : Window
{
    public MainWindow()
    {
       InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Open(object sender, RoutedEventArgs e)
    {
        new Sub().Show(this);
    }
}
