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
using TestApplication;
using Perspex.Rendering;
#if PERSPEX_GTK
using Perspex.Gtk;
#endif
using ReactiveUI;

namespace Perspex.Skia.Desktop.TestApp
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			// The version of ReactiveUI currently included is for WPF and so expects a WPF
			// dispatcher. This makes sure it's initialized.
			System.Windows.Threading.Dispatcher foo = System.Windows.Threading.Dispatcher.CurrentDispatcher;
			new SkiaApp();

			//RendererMixin.DrawFpsCounter = true;

			MainWindow.RootNamespace = "Perspex.Skia.Desktop.TestApp";
			var wnd = MainWindow.Create();

			// let's start small :)
			//var wnd = SimpleWindow.Create();

			DevTools.Attach(wnd);
			Application.Current.Run(wnd);
		}
	}

	internal class SkiaApp : App
	{
		protected override void PlatformInitialization()
		{
			SkiaPlatform.Initialize();
			InitializeSubsystem("Perspex.Win32");
		}
	}

	internal class SimpleWindow
	{
		public static Window Create()
		{
			Border container;

			Window window = new Window
			{
				Title = "Perspex Test Application",
				Content = (container = new Border
				{
					Background = Brushes.Green,
					BorderBrush = Brushes.Yellow,
					BorderThickness = 12,
					Padding = new Thickness(5),
					Child = new Rectangle
					{
						Fill = Brushes.Blue,
						Width = 200,
						Height = 200,
						Margin = new Thickness(50)
					}
				})

			};

			window.Show();
			return window;
		}

	}
}
