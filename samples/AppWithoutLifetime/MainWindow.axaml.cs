using Avalonia;
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

    /// <inheritdoc/>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        this.AttachDevTools();
        base.OnLoaded(e);
    }

    public void Open(object sender, RoutedEventArgs e)
    {
        new Sub().Show(this);
    }
}
