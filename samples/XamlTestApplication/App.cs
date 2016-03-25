// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Logging.Serilog;
using Serilog;

namespace XamlTestApplication
{
    public class App : XamlTestApp
    {
        public App()
        {
            InitializeLogging();
        }

        protected override void RegisterPlatform()
        {
            InitializeSubsystems((int)Environment.OSVersion.Platform);
        }

        private void InitializeLogging()
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
