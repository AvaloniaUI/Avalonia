// -----------------------------------------------------------------------
// <copyright file="GtkPlatform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Gtk
{
    using System;
    using System.Reactive.Disposables;
    using Perspex.Input.Platform;
    using Perspex.Input;
    using Perspex.Platform;
    using Splat;
    using Gtk = global::Gtk;

    public class GtkPlatform : IPlatformThreadingInterface, IPlatformSettings
    {
        private static GtkPlatform instance = new GtkPlatform();

        public GtkPlatform()
        {
            Gtk.Application.Init();
        }

        public Size DoubleClickSize
        {
            get
            {
                // TODO: Is there a setting for this somewhere?
                return new Size(4, 4);
            }
        }

        public TimeSpan DoubleClickTime
        {
            get { return TimeSpan.FromMilliseconds(Gtk.Settings.Default.DoubleClickTime); }
        }

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => new WindowImpl(), typeof(IWindowImpl));
            locator.Register(() => new PopupImpl(), typeof(IPopupImpl));
            locator.Register(() => new ClipboardImpl(), typeof (IClipboard));
            locator.Register(() => CursorFactory.Instance, typeof(IStandardCursorFactory));
            locator.Register(() => GtkKeyboardDevice.Instance, typeof(IKeyboardDevice));
            locator.Register(() => instance, typeof(IPlatformSettings));
            locator.Register(() => instance, typeof(IPlatformThreadingInterface));
            locator.RegisterConstant(new AssetLoader(), typeof(IAssetLoader));
        }

        public bool HasMessages()
        {
            return Gtk.Application.EventsPending();
        }

        public void ProcessMessage()
        {
            Gtk.Application.RunIteration();
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            var result = true;
            var handle = GLib.Timeout.Add(
                (uint)interval.TotalMilliseconds, 
                () =>
                {
                    tick();
                    return result;
                });

            return Disposable.Create(() => result = false);
        }

        public void Wake()
        {
        }
    }
}