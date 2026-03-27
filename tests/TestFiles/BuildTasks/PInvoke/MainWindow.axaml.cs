using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PInvoke;

public partial class MainWindow : Window
{
    [DllImport(@"libhello")]
    extern static int add(int a, int b);

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        var x = add(1, 2);
    }
}
