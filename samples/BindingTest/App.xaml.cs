using System;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
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

            Log.Logger = new LoggerConfiguration()
                .Filter.ByIncludingOnly(Matching.WithProperty("Area", "Property"))
                .Filter.ByIncludingOnly(Matching.WithProperty("Property", "Text"))
                .MinimumLevel.Verbose()
                .WriteTo.Trace(outputTemplate: "[{Id:X8}] [{SourceContext}] {Message}")
                .CreateLogger();
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
    }
}
