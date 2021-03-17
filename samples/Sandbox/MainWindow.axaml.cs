using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Sandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            ((Button)this.Content).Click += MainWindow_Click;
        }

        private void MainWindow_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((Button)sender);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
