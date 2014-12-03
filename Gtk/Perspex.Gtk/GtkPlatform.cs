// -----------------------------------------------------------------------
// <copyright file="IDescription.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Gtk
{
	using System;
	using System.Reactive.Disposables;
	using Perspex.Platform;
	using Splat;
	using Gtk = global::Gtk;

	public class GtkPlatform : IPlatformThreadingInterface
	{
		private static GtkPlatform instance = new GtkPlatform ();

		public GtkPlatform ()
		{
			Gtk.Application.Init();
		}

		public static void Initialize()
		{
			var locator = Locator.CurrentMutable;
            locator.Register(() => new WindowImpl(), typeof(IWindowImpl));
            //locator.Register(() => WindowsKeyboardDevice.Instance, typeof(IKeyboardDevice));
            locator.Register(() => instance, typeof(IPlatformThreadingInterface));
		}

		public void ProcessMessage ()
		{
			Gtk.Application.RunIteration();
		}

		public IDisposable StartTimer (TimeSpan interval, Action tick)
		{
            var result = true;
            var handle = GLib.Timeout.Add((uint)interval.TotalMilliseconds, () =>
            {
                tick();
                return result;
            });

            return Disposable.Create(() => result = false);
		}

		public void Wake ()
		{
		}
	}
}

