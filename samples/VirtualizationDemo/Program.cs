// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Serilog;

namespace VirtualizationDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
               .UsePlatformDetect()
               .UseReactiveUI()
               .LogToDebug()
               .Start<MainWindow>();
        }
    }
}
