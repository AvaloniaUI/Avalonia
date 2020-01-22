using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace NativeEmbedSample
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachDevTools();
        }

        public async void ShowPopupDelay(object sender, RoutedEventArgs args)
        {
            await Task.Delay(3000);
            ShowPopup(sender, args);
        }

        public void ShowPopup(object sender, RoutedEventArgs args)
        {

            new ContextMenu()
            {
                Items = new List<MenuItem>
                {
                    new MenuItem() { Header = "Test" }, new MenuItem() { Header = "Test" }
                }
            }.Open((Control)sender);
        }
    }
}
