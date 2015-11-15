// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.IO;
using System.Reactive.Linq;
using Perspex;
using Perspex.Animation;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Html;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Diagnostics;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
#if PERSPEX_GTK
using Perspex.Gtk;
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
            new App();
            var wnd = MainWindow.Create();
            DevTools.Attach(wnd);
            Application.Current.Run(wnd);
        }
    }
}
