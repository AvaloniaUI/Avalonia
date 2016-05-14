// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.IO;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Html;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
#if AVALONIA_GTK
using Avalonia.Gtk;
#endif
using ReactiveUI;

namespace TestApplication
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // The version of ReactiveUI currently included is for WPF and so expects a WPF
            // dispatcher. This makes sure it's initialized.
            System.Windows.Threading.Dispatcher foo = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            var app = new App();

            AppBuilder.Configure(app)
                .UseWin32()
                .UseDirect2D1()
                .SetupWithoutStarting();

            app.Run();
        }
    }
}
