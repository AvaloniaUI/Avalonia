using System;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Themes.Default;

namespace BindingTest
{
    public class App : Application
    {
        public App()
        {
            RegisterServices();
            InitializeSubsystems((int)Environment.OSVersion.Platform);
            Styles = new DefaultTheme();
        }

        public static void AttachDevTools(Window window)
        {
            DevTools.Attach(window);
        }

        private static void Main()
        {
            var app = new App();
            var window = new MainWindow();
            window.Show();
            app.Run(window);
        }
    }
}
