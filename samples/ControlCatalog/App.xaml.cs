using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog
{
    public class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AvaloniaXamlLoader.Load(this);

            var mainWindow = new MainWindow();

            mainWindow.Show();
        }
    }
}
