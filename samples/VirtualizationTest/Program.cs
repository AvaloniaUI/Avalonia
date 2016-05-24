// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia;
using Avalonia.Controls;

namespace VirtualizationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
               .UseWin32()
               .UseDirect2D1()
               .Start<MainWindow>();
        }
    }
}
