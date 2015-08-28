using System;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Themes.Default;

namespace ApplicationTemplate
{
    public class App : Application
    {
        public App()
        {
            this.RegisterServices();
            this.InitializeSubsystems((int)Environment.OSVersion.Platform);
            this.Styles = new DefaultTheme();
        }

        public static void AttachDevTools(Window window)
        {
#if DEBUG
            DevTools.Attach(this);
#endif
        }

        static void Main(string[] args)
        {
            var app = new App();
            var window = new MainWindow();
            window.Show();
            app.Run(window);
        }
    }
}
