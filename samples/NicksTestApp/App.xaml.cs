using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace NicksTestApp
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        //public override void RegisterServices()
        //{
        //    base.RegisterServices();

        //    // replacing the styler implementation
        //    AvaloniaLocator.CurrentMutable.Bind<IStyler>().ToConstant(new MyStyler());
        //}
    }
}
