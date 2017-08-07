// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Serilog;

namespace VirtualizationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            InitializeLogging();

            AppBuilder.Configure<App>()
               .UsePlatformDetect()
               .Start<MainWindow>();
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
