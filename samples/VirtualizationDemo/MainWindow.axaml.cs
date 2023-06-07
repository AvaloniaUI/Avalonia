using Avalonia;
using Avalonia.Controls;
using VirtualizationDemo.ViewModels;

namespace VirtualizationDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.AttachDevTools();
        DataContext = new MainWindowViewModel();
    }
}
