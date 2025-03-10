using BindingDemo.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BindingDemo
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            Resources["SharedItem"] = new MainWindowViewModel.TestItem<string>() { Value = "shared" };
            this.InitializeComponent();
            this.DataContext = new MainWindowViewModel();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
