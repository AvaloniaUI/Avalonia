// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Diagnostics;
using System.Windows.Threading;
using Perspex;
using Serilog;
using Perspex.Logging.Serilog;

namespace XamlTestApplication
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // this sucks. Can we fix this? Do we even need it anymore?
            var foo = Dispatcher.CurrentDispatcher;

            InitializeLogging();

            new XamlTestApp()
                   .UseWin32()
                   .UseDirect2D()
                   .LoadFromXaml()
                   .RunWithMainWindow<Views.MainWindow>();
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