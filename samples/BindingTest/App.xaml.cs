using System;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Logging.Serilog;
using Perspex.Markup.Xaml;
using Serilog;

namespace BindingTest
{
    public class App : Application
    {
        public App()
        {
            RegisterServices();
        }

        private static void Main()
        {
			InitializeLogging();

			new App()
				.UseWin32Subsystem()
				.UseDirect2D()
				.LoadFromXaml()
				.RunWithMainWindow<MainWindow>();
        }

		private static void InitializeLogging()
        {
#if DEBUG
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
#endif
        }
    }
}
