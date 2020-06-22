using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VirtualizationDemo.ViewModels;

namespace VirtualizationDemo
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            DataContext = new MainWindowViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
