// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Diagnostics;
#if NOT_NETSTANDARD
using System.Windows.Threading;
#endif
using Avalonia;
using Serilog;
using Avalonia.Logging.Serilog;
using Avalonia.Controls;

namespace XamlTestApplication
{
    internal class Program
    {
        private static void Main(string[] args)
        {
#if NOT_NETSTANDARD
            // this sucks. Can we fix this? Do we even need it anymore?
            var foo = Dispatcher.CurrentDispatcher;
#endif

            InitializeLogging();

            AppBuilder.Configure<XamlTestApp>()
                .UsePlatformDetect()
                .Start<Views.MainWindow>();
        }

        private static void InitializeLogging()
        {
#if DEBUG && NOT_NETSTANDARD
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
#endif
        }
    }
}