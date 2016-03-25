using System;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Logging;
using Perspex.Logging.Serilog;
using Perspex.Markup.Xaml;
using Serilog;
using Serilog.Filters;

namespace BindingTest
{
    public class App : Application
    {
        public App()
        {
            RegisterServices();
            InitializeSubsystems((int)Environment.OSVersion.Platform);
            InitializeComponent();
            InitializeLogging();
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

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }

        private void InitializeLogging()
        {
#if DEBUG
            SerilogLogger.Initialize(new LoggerConfiguration()
                .Filter.ByIncludingOnly(Matching.WithProperty("Area", LogArea.Layout))
                .MinimumLevel.Verbose()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
#endif
        }
    }
}
