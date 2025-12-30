using Avalonia.Controls;
using VirtualizationDemo.ViewModels;

namespace VirtualizationDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
