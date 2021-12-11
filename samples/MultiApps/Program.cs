using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace Previewer
{

    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = AppBuilder.Configure<Application>()
                                    .AllowMultipleStarts()
                                    .UsePlatformDetect()
                                    .UseFluentTheme();

            await Task.Run(() =>
            {
                using (builder.StartWithClassicDesktopLifetime(desktop =>
                {
                    var window = new Window { Title = "Minimal Avalonia" };
                    window.Content = new TextBox { [!!TextBlock.TextProperty] = window[!!Window.TitleProperty] };
                    desktop.MainWindow = window;
                }))
                {

                }
            });

            await Task.Run(() =>
            {
                using (builder.StartWithClassicDesktopLifetime(desktop =>
                {
                    var window = new Window { Title = "Minimal Avalonia 2" };
                    window.Content = new TextBox { [!!TextBlock.TextProperty] = window[!!Window.TitleProperty] };
                    desktop.MainWindow = window;
                }))
                {

                }
            });
        }
    }
}
