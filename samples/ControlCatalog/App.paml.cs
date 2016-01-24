using System;
using System.Linq;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Markup.Xaml;
using Perspex.Themes.Default;

namespace ControlCatalog
{
    class App : Application
    {
        public App()
        {
            RegisterServices();
            InitializeSubsystems(GetPlatformId());
            Styles = new DefaultTheme();
            InitializeComponent();
        }

        public static void AttachDevTools(Window window)
        {
#if DEBUG
            DevTools.Attach(window);
#endif
        }

        static void Main(string[] args)
        {
            var app = new App();
            var window = new MainWindow();
            window.Show();
            app.Run(window);
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }

        private int GetPlatformId()
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Contains("--gtk"))
            {
                return (int)PlatformID.Unix;
            }
            else
            {
                return (int)Environment.OSVersion.Platform;
            }
        }
    }
}
