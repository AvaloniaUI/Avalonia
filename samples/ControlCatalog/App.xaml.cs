using Avalonia;
using Avalonia.Markup.Xaml;

namespace ControlCatalog
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnStartup()
        {
            base.OnStartup();

            var mainWindow = new MainWindow();

            mainWindow.Show();
        }
    }
}
