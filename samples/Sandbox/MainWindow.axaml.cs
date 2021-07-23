using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Native;

namespace Sandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void DisableCloseButton_OnClick(object? sender, RoutedEventArgs e)
        {
            DisableCloseButton();
        }

        private void EnableCloseButton_OnClick(object? sender, RoutedEventArgs e)
        {
            EnableCloseButton();
        }
    }
}
